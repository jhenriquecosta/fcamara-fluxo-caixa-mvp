using System.Globalization;
using System.Text.Json;
using Cashflow.Consolidado.Application;
using Cashflow.Consolidado.Domain;
using Cashflow.Consolidado.Infrastructure.Observability;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Cashflow.Consolidado.Infrastructure.Messaging;

public sealed class ConsolidadoConsumer(
    NpgsqlDataSource dataSource,
    IConfiguration configuration,
    ConsolidadoMetrics metrics,
    ProjetarEventoHandler projecao,
    ILogger<ConsolidadoConsumer> logger) : BackgroundService
{
    private static readonly TimeSpan[] Tentativas =
    {
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4)
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bootstrap = configuration["Kafka:BootstrapServers"];
        if (string.IsNullOrWhiteSpace(bootstrap))
        {
            logger.LogWarning("Kafka não configurado — consumidor do consolidado desligado");
            return;
        }

        var topic = configuration["Kafka:Topic"] ?? "cashflow.lancamentos.v1";
        var dltTopic = configuration["Kafka:DltTopic"] ?? "cashflow.lancamentos.v1.dlt";

        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrap,
            GroupId = configuration["Kafka:GroupId"] ?? "consolidado",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        using var producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = bootstrap,
            Acks = Acks.All
        }).Build();

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

            RegistrarLag(consumer, resultado);

            var commit = true;
            var processado = false;
            for (var tentativa = 0; tentativa < 4 && !processado; tentativa++)
            {
                try
                {
                    await ProcessarAsync(resultado.Message.Value, stoppingToken);
                    processado = true;
                }
                catch (Exception ex)
                {
                    if (tentativa < 3)
                    {
                        logger.LogWarning(ex, "tentativa {Tentativa} de 4 falhou; tentando de novo", tentativa + 1);
                        try { await Task.Delay(Tentativas[tentativa], stoppingToken); }
                        catch (OperationCanceledException) { return; }
                    }
                    else
                    {
                        logger.LogError(ex, "falha persistente; enviando o evento para a DLT {DltTopic}", dltTopic);
                        try
                        {
                            await producer.ProduceAsync(dltTopic, new Message<string, string>
                            {
                                Key = resultado.Message.Key,
                                Value = resultado.Message.Value
                            }, stoppingToken);
                            metrics.Dlt();
                        }
                        catch (Exception dltEx)
                        {
                            logger.LogError(dltEx, "falha ao enviar para a DLT; offset não commitado");
                            commit = false;
                        }
                    }
                }
            }

            if (commit)
                consumer.Commit(resultado);
        }

        consumer.Close();
    }

    private void RegistrarLag(IConsumer<string, string> consumer, ConsumeResult<string, string> resultado)
    {
        try
        {
            var watermark = consumer.QueryWatermarkOffsets(resultado.TopicPartition, TimeSpan.FromSeconds(5));
            var commitado = consumer.Committed(new[] { resultado.TopicPartition }, TimeSpan.FromSeconds(5))
                .FirstOrDefault();
            var lag = watermark.High.Value - (commitado?.Offset.Value ?? watermark.Low.Value);
            metrics.Lag(Math.Max(0, lag));
        }
        catch (KafkaException)
        {
            // métricas são observação; a ausência de lag não interrompe o consumo
        }
    }

    private async Task ProcessarAsync(string json, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var eventId = root.GetProperty("eventId").GetGuid();
        var payload = root.GetProperty("payload");
        var comerciante = payload.GetProperty("comercianteId").GetGuid();
        var tipo = payload.GetProperty("tipo").GetString() ?? "debito";
        var valor = payload.GetProperty("valor").GetDecimal();
        var data = DateOnly.ParseExact(
            payload.GetProperty("data").GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture);

        await projecao.HandleAsync(new LancamentoEvento(eventId, comerciante, tipo, valor, data), ct);
    }
}
