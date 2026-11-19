using Cashflow.Infrastructure.MessageBrokers.Kafka;

namespace Cashflow.Infrastructure.MessageBrokers;

/// <summary>A seção <c>MessageBroker</c> da configuração.</summary>
public sealed class MessageBrokerOptions
{
    /// <summary>O broker em uso. Só o Kafka está implementado (ADR-0002); a referência traz outros.</summary>
    public string Provider { get; set; } = "Kafka";

    /// <summary>A configuração do Kafka.</summary>
    public KafkaOptions Kafka { get; set; } = new();

    /// <summary>Diz se o broker configurado é o Kafka.</summary>
    public bool UsesKafka() => string.Equals(Provider, "Kafka", StringComparison.OrdinalIgnoreCase);
}
