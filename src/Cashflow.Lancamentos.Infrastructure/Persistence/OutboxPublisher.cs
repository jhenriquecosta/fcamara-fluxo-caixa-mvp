using System.Text.Json;
using Cashflow.Lancamentos.Infrastructure.Observability;
using Confluent.Kafka;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Cashflow.Lancamentos.Infrastructure.Persistence;

public sealed class OutboxPublisher(
    NpgsqlDataSource dataSource,
    IConfiguration configuration,
    LancamentosMetrics metrics,
    ILogger<OutboxPublisher> logger) : BackgroundService
{
    private sealed record OutboxRow(Guid Id, string Payload);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bootstrap = configuration["Kafka:BootstrapServers"];
        if (string.IsNullOrWhiteSpace(bootstrap))
        {
            logger.LogWarning("Kafka não configurado — publicador do outbox desligado");
            return;
        }

        var topic = configuration["Kafka:Topic"] ?? "cashflow.lancamentos.v1";
        using var producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = bootstrap,
            Acks = Acks.All,
            EnableIdempotence = true
        }).Build();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var conn = await dataSource.OpenConnectionAsync(stoppingToken);
                await using var tx = await conn.BeginTransactionAsync(stoppingToken);

                var pendentes = (await conn.QueryAsync<OutboxRow>(
                    "SELECT id AS Id, payload::text AS Payload FROM outbox_event " +
                    "WHERE publicado_em IS NULL ORDER BY criado_em LIMIT 100 FOR UPDATE SKIP LOCKED",
                    transaction: tx)).ToList();

                foreach (var row in pendentes)
                {
                    var chave = ExtrairChave(row.Payload);
                    await producer.ProduceAsync(topic,
                        new Message<string, string> { Key = chave, Value = row.Payload }, stoppingToken);
                    await conn.ExecuteAsync(
                        "UPDATE outbox_event SET publicado_em = now() WHERE id = @id",
                        new { id = row.Id }, tx);
                }

                await tx.CommitAsync(stoppingToken);

                var idade = await conn.ExecuteScalarAsync<double>("""
                    SELECT COALESCE(EXTRACT(EPOCH FROM (now() - MIN(criado_em))), 0)::float8
                    FROM outbox_event WHERE publicado_em IS NULL
                    """);
                metrics.OutboxAge(idade);

                if (pendentes.Count == 0)
                    await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "falha no publicador do outbox; tentando de novo em 1 s");
                try { await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken); }
                catch (OperationCanceledException) { break; }
            }
        }
    }

    private static string ExtrairChave(string payload)
    {
        using var doc = JsonDocument.Parse(payload);
        return doc.RootElement.GetProperty("payload").GetProperty("comercianteId").GetString() ?? string.Empty;
    }
}
