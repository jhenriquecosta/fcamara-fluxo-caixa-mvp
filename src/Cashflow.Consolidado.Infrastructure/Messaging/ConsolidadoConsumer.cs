using Cashflow.Common.Messaging;
using Cashflow.Contracts;
using Cashflow.Consolidado.Application;
using Cashflow.Consolidado.Domain;
using Cashflow.Consolidado.Infrastructure.Observability;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Cashflow.Consolidado.Infrastructure.Messaging;

public sealed class ConsolidadoConsumer(
    NpgsqlDataSource dataSource,
    KafkaOptions kafka,
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
        if (!kafka.Habilitado)
        {
            logger.LogWarning("Kafka não configurado — consumidor do consolidado desligado");
            return;
        }

        using var consumer = CashflowKafka.CriarConsumidor(kafka);
        using var producer = CashflowKafka.CriarProdutorDlt(kafka);

        consumer.Subscribe(kafka.Topic);

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
                        logger.LogError(ex, "falha persistente; enviando o evento para a DLT {DltTopic}", kafka.DltTopic);
                        try
                        {
                            await producer.ProduceAsync(kafka.DltTopic, new Message<string, string>
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
        var evento = CashflowEventSerializer.Deserialize<LancamentoEventPayload>(json)
            ?? throw new InvalidOperationException("evento inválido no tópico");
        var payload = evento.Payload;

        await projecao.HandleAsync(new LancamentoEvento(
            evento.EventId, payload.ComercianteId, payload.Tipo, payload.Valor, payload.Data), ct);
    }
}
