using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Dapper;
using Npgsql;

namespace Cashflow.Consolidado.Api.Application;

public sealed class ConsolidadoConsumer(
    NpgsqlDataSource dataSource,
    IConfiguration configuration,
    ILogger<ConsolidadoConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bootstrap = configuration["Kafka:BootstrapServers"];
        if (string.IsNullOrWhiteSpace(bootstrap))
        {
            logger.LogWarning("Kafka não configurado — consumidor do consolidado desligado");
            return;
        }

        var topic = configuration["Kafka:Topic"] ?? "cashflow.lancamentos.v1";
        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrap,
            GroupId = configuration["Kafka:GroupId"] ?? "consolidado",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, string>? resultado = null;
            try
            {
                resultado = consumer.Consume(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException ex)
            {
                logger.LogError(ex, "erro ao consumir a mensagem");
                continue;
            }

            if (resultado is null || resultado.IsPartitionEOF)
                continue;

            try
            {
                await ProcessarAsync(resultado.Message.Value, stoppingToken);
                consumer.Commit(resultado);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "falha ao processar o evento; sem commit (retries e DLT entram na fatia 4)");
                try { await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken); }
                catch (OperationCanceledException) { break; }
            }
        }

        consumer.Close();
    }

    private async Task ProcessarAsync(string json, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var eventId = root.GetProperty("eventId").GetGuid();
        var payload = root.GetProperty("payload");
        var comerciante = payload.GetProperty("comercianteId").GetGuid();
        var tipo = payload.GetProperty("tipo").GetString();
        var valor = payload.GetProperty("valor").GetDecimal();
        var data = DateOnly.ParseExact(payload.GetProperty("data").GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var delta = tipo == "credito" ? valor : -valor;

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        var inserido = await conn.ExecuteAsync("""
            INSERT INTO evento_processado (event_id, processado_em)
            VALUES (@eventId, now())
            ON CONFLICT (event_id) DO NOTHING
            """, new { eventId }, tx);

        if (inserido == 0)
        {
            await tx.CommitAsync(ct);
            logger.LogInformation("evento repetido ignorado {EventId} {ComercianteHash}",
                eventId, HashComerciante(comerciante));
            return;
        }

        await conn.ExecuteAsync("""
            INSERT INTO consolidado_diario (comerciante_id, data, saldo_dia, saldo_acumulado)
            VALUES (@comerciante, @data, @delta,
                    COALESCE((SELECT saldo_acumulado FROM consolidado_diario
                              WHERE comerciante_id = @comerciante AND data < @data
                              ORDER BY data DESC LIMIT 1), 0) + @delta)
            ON CONFLICT (comerciante_id, data) DO UPDATE
            SET saldo_dia = consolidado_diario.saldo_dia + EXCLUDED.saldo_dia,
                saldo_acumulado = consolidado_diario.saldo_acumulado + EXCLUDED.saldo_dia
            """, new { comerciante, data, delta }, tx);

        await tx.CommitAsync(ct);

        logger.LogInformation("evento aplicado {EventId} {ComercianteHash} {Data} {Delta}",
            eventId, HashComerciante(comerciante), data, delta);
    }

    private static string HashComerciante(Guid id) =>
        Convert.ToHexString(SHA256.HashData(id.ToByteArray()))[..16].ToLowerInvariant();
}
