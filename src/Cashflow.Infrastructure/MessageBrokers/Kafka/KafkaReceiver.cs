using Cashflow.Contracts;
using Cashflow.Domain.Infrastructure.MessageBrokers;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace Cashflow.Infrastructure.MessageBrokers.Kafka;

/// <summary>
/// Consome um tópico garantindo que nenhum evento se perca em silêncio (ADR-0005): cada mensagem tem
/// quatro tentativas (com 1 s, 2 s e 4 s entre elas) e, esgotadas, vai para a DLT. O offset só avança
/// depois de processar ou de desviar para a DLT.
/// </summary>
/// <remarks>
/// Commit manual e entrega at-least-once: uma queda entre processar e commitar reentrega a mensagem,
/// e quem consome precisa ser idempotente. O consolidado é, pelo <c>eventId</c>.
/// </remarks>
public sealed class KafkaReceiver<TConsumer, T> : IMessageReceiver<TConsumer, T>, IDisposable
{
    private static readonly TimeSpan[] WaitBeforeRetry =
    {
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4)
    };

    private static readonly TimeSpan BrokerQueryTimeout = TimeSpan.FromSeconds(5);

    /// <summary>De quanto em quanto tempo, no máximo, o lag de uma partição é perguntado ao broker.</summary>
    private static readonly long LagSampleIntervalMs = (long)TimeSpan.FromSeconds(5).TotalMilliseconds;

    /// <summary>Quando cada partição teve o lag medido; o laço de consumo é de uma thread só.</summary>
    private readonly Dictionary<TopicPartition, long> _lastLagSample = new();

    private readonly IConsumer<string?, string> _consumer;
    private readonly IProducer<string?, string> _deadLetterProducer;
    private readonly string _deadLetterTopic;
    private readonly MessagingMetrics _metrics;
    private readonly ILogger _logger;

    /// <summary>Cria o consumidor já assinando <paramref name="topic"/> e o producer da DLT.</summary>
    public KafkaReceiver(string bootstrapServers, string topic, string deadLetterTopic, string groupId,
        MessagingMetrics metrics, ILogger<KafkaReceiver<TConsumer, T>> logger)
    {
        _deadLetterTopic = deadLetterTopic;
        _metrics = metrics;
        _logger = logger;

        _consumer = new ConsumerBuilder<string?, string>(new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        }).Build();

        _deadLetterProducer = new ProducerBuilder<string?, string>(new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All
        }).Build();

        _consumer.Subscribe(topic);
    }

    /// <summary>
    /// O laço de consumo, até o cancelamento. Uma mensagem só é commitada quando foi processada ou
    /// guardada na DLT; se nem a DLT aceitou, o offset fica e a mensagem volta na próxima leitura.
    /// </summary>
    public async Task ReceiveAsync(Func<T, MetaData, Task> action, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            ConsumeResult<string?, string> consumed;
            try
            {
                consumed = _consumer.Consume(cancellationToken);
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

            if (consumed is null || consumed.IsPartitionEOF)
                continue;

            RecordLag(consumed);

            if (await TryProcessWithRetriesAsync(consumed, action, cancellationToken)
                || await TrySendToDeadLetterAsync(consumed, cancellationToken))
            {
                _consumer.Commit(consumed);
            }
        }

        _consumer.Close();
    }

    /// <summary>Tenta processar até esgotar <see cref="WaitBeforeRetry"/>. Falso quando a falha persiste.</summary>
    private async Task<bool> TryProcessWithRetriesAsync(
        ConsumeResult<string?, string> consumed, Func<T, MetaData, Task> action, CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                var (payload, metaData) = OpenEnvelope(consumed.Message.Value);
                await action(payload, metaData);
                return true;
            }
            catch (Exception ex) when (attempt < WaitBeforeRetry.Length && !ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "tentativa {Tentativa} de {Total} falhou; tentando de novo",
                    attempt + 1, WaitBeforeRetry.Length + 1);
                await Task.Delay(WaitBeforeRetry[attempt], ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "falha persistente; enviando o evento para a DLT {DltTopic}", _deadLetterTopic);
                return false;
            }
        }
    }

    /// <summary>Copia a mensagem original, com a mesma chave, para a DLT. Falso se a DLT recusou.</summary>
    private async Task<bool> TrySendToDeadLetterAsync(ConsumeResult<string?, string> consumed, CancellationToken ct)
    {
        try
        {
            await _deadLetterProducer.ProduceAsync(_deadLetterTopic, new Message<string?, string>
            {
                Key = consumed.Message.Key,
                Value = consumed.Message.Value
            }, ct);
            _metrics.CountDeadLetter();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "falha ao enviar para a DLT; offset não commitado");
            return false;
        }
    }

    /// <summary>Tira o payload e os metadados do envelope <see cref="CashflowEvent{T}"/>. Mensagem malformada lança.</summary>
    private static (T Payload, MetaData MetaData) OpenEnvelope(string json)
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

    /// <summary>
    /// Mede quanto falta consumir na partição (último offset menos o commitado). A métrica é observação:
    /// se o broker não responder, o consumo segue sem ela.
    /// </summary>
    /// <remarks>
    /// A medição custa duas idas ao broker (watermark e offset commitado). Feita a cada mensagem, somaria
    /// dois RTTs a cada evento; por isso cada partição é medida no máximo uma vez a cada 5 s, o que basta
    /// para um alerta que olha janelas de minutos.
    /// </remarks>
    private void RecordLag(ConsumeResult<string?, string> consumed)
    {
        var agora = Environment.TickCount64;
        if (_lastLagSample.TryGetValue(consumed.TopicPartition, out var ultima) && agora - ultima < LagSampleIntervalMs)
            return;
        _lastLagSample[consumed.TopicPartition] = agora;

        try
        {
            var watermark = _consumer.QueryWatermarkOffsets(consumed.TopicPartition, BrokerQueryTimeout);
            var committed = _consumer.Committed(new[] { consumed.TopicPartition }, BrokerQueryTimeout)
                .FirstOrDefault();
            var lag = watermark.High.Value - (committed?.Offset.Value ?? watermark.Low.Value);
            _metrics.RecordConsumerLag(Math.Max(0, lag));
        }
        catch (KafkaException)
        {
            // sem a métrica o consumo continua
        }
    }

    /// <summary>Libera o consumidor e o producer da DLT.</summary>
    public void Dispose()
    {
        _consumer.Dispose();
        _deadLetterProducer.Dispose();
    }
}
