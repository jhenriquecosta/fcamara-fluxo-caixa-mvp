namespace Cashflow.Infrastructure.MessageBrokers.Kafka;

/// <summary>A seção <c>MessageBroker:Kafka</c> da configuração.</summary>
public sealed class KafkaOptions
{
    /// <summary>Os brokers, como <c>kafka:9092</c>.</summary>
    public string BootstrapServers { get; set; } = string.Empty;

    /// <summary>O consumer group: réplicas no mesmo grupo dividem as partições.</summary>
    public string GroupId { get; set; } = string.Empty;

    /// <summary>O tópico de cada mensagem, pela chave do nome do tipo (<c>typeof(T).Name</c>), como na referência.</summary>
    public Dictionary<string, string> Topics { get; set; } = new();

    /// <summary>A DLT de cada mensagem: o evento que falha em todas as tentativas vai para cá.</summary>
    public Dictionary<string, string> DeadLetterTopics { get; set; } = new();
}
