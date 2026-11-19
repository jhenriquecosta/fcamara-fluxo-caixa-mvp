using Cashflow.Contracts;
using Cashflow.Domain.Infrastructure.MessageBrokers;
using Confluent.Kafka;

namespace Cashflow.Infrastructure.MessageBrokers.Kafka;

/// <summary>
/// Publica no tópico com o envelope <see cref="CashflowEvent{T}"/> — o schema do barramento
/// (Contracts) — e com a chave de partição do <see cref="MetaData"/>, que mantém a ordem por comerciante.
/// </summary>
public sealed class KafkaSender<T> : IMessageSender<T>, IDisposable
{
    private readonly string _topic;
    private readonly IProducer<string?, string> _producer;

    public KafkaSender(string bootstrapServers, string topic)
    {
        _topic = topic;
        _producer = new ProducerBuilder<string?, string>(new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true
        }).Build();
    }

    public async Task SendAsync(T message, MetaData? metaData = null, CancellationToken cancellationToken = default)
    {
        metaData ??= new MetaData { MessageId = Guid.NewGuid().ToString() };

        var envelope = new CashflowEvent<T>(
            Guid.Parse(metaData.MessageId),
            metaData.EventType ?? typeof(T).Name,
            metaData.CreationDateTime ?? DateTimeOffset.UtcNow,
            int.TryParse(metaData.MessageVersion, out var versao) ? versao : 1,
            message);

        await _producer.ProduceAsync(_topic, new Message<string?, string>
        {
            Key = metaData.PartitionKey,
            Value = CashflowEventSerializer.Serialize(envelope)
        }, cancellationToken);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
    }
}
