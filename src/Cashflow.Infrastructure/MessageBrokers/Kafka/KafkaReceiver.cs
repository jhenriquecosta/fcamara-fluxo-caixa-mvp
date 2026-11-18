using Cashflow.Contracts;
using Cashflow.Domain.Infrastructure.MessageBrokers;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace Cashflow.Infrastructure.MessageBrokers.Kafka;

/// <summary>
/// Consome com commit manual: cada mensagem tem quatro tentativas (1 s, 2 s, 4 s entre elas) e,
/// esgotadas, vai para a DLT. O offset só avança depois do processamento ou da DLT — nenhum
/// evento se perde em silêncio (ADR-0005).
/// </summary>
public sealed class KafkaReceiver<TConsumer, T> : IMessageReceiver<TConsumer, T>, IDisposable
{
    private static readonly TimeSpan[] Esperas =
    {
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4)
    };

    private readonly IConsumer<string?, string> _consumer;
    private readonly IProducer<string?, string> _dlt;
    private readonly string _dltTopic;
    private readonly MessagingMetrics _metrics;
    private readonly ILogger _logger;

    public KafkaReceiver(string bootstrapServers, string topic, string dltTopic, string groupId,
        MessagingMetrics metrics, ILogger<KafkaReceiver<TConsumer, T>> logger)
    {
        _dltTopic = dltTopic;
        _metrics = metrics;
        _logger = logger;

        _consumer = new ConsumerBuilder<string?, string>(new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        }).Build();

        _dlt = new ProducerBuilder<string?, string>(new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All
        }).Build();

        _consumer.Subscribe(topic);
    }

    public async Task ReceiveAsync(Func<T, MetaData, Task> action, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            ConsumeResult<string?, string> resultado;
            try
            {
                resultado = _consumer.Consume(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "erro ao consumir a mensagem");
                continue;
            }

            if (resultado is null || resultado.IsPartitionEOF)
                continue;

            RegistrarLag(resultado);

            if (await ProcessarComRetentativasAsync(resultado, action, cancellationToken)
                || await EnviarParaDltAsync(resultado, cancellationToken))
            {
                _consumer.Commit(resultado);
            }
        }

        _consumer.Close();
    }

    private async Task<bool> ProcessarComRetentativasAsync(
        ConsumeResult<string?, string> resultado, Func<T, MetaData, Task> action, CancellationToken ct)
    {
        for (var tentativa = 0; ; tentativa++)
        {
            try
            {
                var (dados, metaData) = Desembrulhar(resultado.Message.Value);
                await action(dados, metaData);
                return true;
            }
            catch (Exception ex) when (tentativa < Esperas.Length && !ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "tentativa {Tentativa} de {Total} falhou; tentando de novo",
                    tentativa + 1, Esperas.Length + 1);
                await Task.Delay(Esperas[tentativa], ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "falha persistente; enviando o evento para a DLT {DltTopic}", _dltTopic);
                return false;
            }
        }
    }

    private async Task<bool> EnviarParaDltAsync(ConsumeResult<string?, string> resultado, CancellationToken ct)
    {
        try
        {
            await _dlt.ProduceAsync(_dltTopic, new Message<string?, string>
            {
                Key = resultado.Message.Key,
                Value = resultado.Message.Value
            }, ct);
            _metrics.Dlt();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "falha ao enviar para a DLT; offset não commitado");
            return false;
        }
    }

    private static (T Dados, MetaData MetaData) Desembrulhar(string json)
    {
        var envelope = CashflowEventSerializer.Deserialize<T>(json)
                       ?? throw new InvalidOperationException("mensagem vazia no tópico");

        return (envelope.Payload, new MetaData
        {
            MessageId = envelope.EventId.ToString(),
            MessageVersion = envelope.Version.ToString(),
            EventType = envelope.EventType,
            CreationDateTime = envelope.OccurredAt
        });
    }

    private void RegistrarLag(ConsumeResult<string?, string> resultado)
    {
        try
        {
            var watermark = _consumer.QueryWatermarkOffsets(resultado.TopicPartition, TimeSpan.FromSeconds(5));
            var commitado = _consumer.Committed(new[] { resultado.TopicPartition }, TimeSpan.FromSeconds(5))
                .FirstOrDefault();
            var lag = watermark.High.Value - (commitado?.Offset.Value ?? watermark.Low.Value);
            _metrics.Lag(Math.Max(0, lag));
        }
        catch (KafkaException)
        {
            // a métrica é observação; sem ela o consumo continua
        }
    }

    public void Dispose()
    {
        _consumer.Dispose();
        _dlt.Dispose();
    }
}
