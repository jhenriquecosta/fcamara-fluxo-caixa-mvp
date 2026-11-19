namespace Cashflow.Infrastructure.MessageBrokers.Kafka;

public sealed class KafkaOptions
{
    public string BootstrapServers { get; set; } = string.Empty;

    public string GroupId { get; set; } = string.Empty;

    /// <summary>O tópico de cada mensagem, pela chave do nome do tipo (<c>typeof(T).Name</c>), como na referência.</summary>
    public Dictionary<string, string> Topics { get; set; } = new();

    /// <summary>A DLT de cada mensagem: o evento que falha em todas as tentativas vai para cá.</summary>
    public Dictionary<string, string> DeadLetterTopics { get; set; } = new();
}
