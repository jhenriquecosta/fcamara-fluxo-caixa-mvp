using Cashflow.Infrastructure.MessageBrokers.Kafka;

namespace Cashflow.Infrastructure.MessageBrokers;

public sealed class MessageBrokerOptions
{
    /// <summary>O broker em uso. Só o Kafka está implementado (ADR-0002); a referência traz outros.</summary>
    public string Provider { get; set; } = "Kafka";

    public KafkaOptions Kafka { get; set; } = new();

    public bool UsedKafka() => string.Equals(Provider, "Kafka", StringComparison.OrdinalIgnoreCase);
}
