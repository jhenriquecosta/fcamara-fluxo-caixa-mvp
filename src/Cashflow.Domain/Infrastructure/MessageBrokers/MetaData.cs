namespace Cashflow.Domain.Infrastructure.MessageBrokers;

public sealed class MetaData
{
    public string MessageId { get; set; } = string.Empty;

    public string MessageVersion { get; set; } = "1";

    public string? CorrelationId { get; set; }

    public DateTimeOffset? CreationDateTime { get; set; }

    public DateTimeOffset? EnqueuedDateTime { get; set; }

    /// <summary>O tipo do evento no barramento (<c>lancamento.registrado</c>, ...). Acréscimo à referência:
    /// o envelope publicado carrega o tipo, e um mesmo payload serve a mais de um evento.</summary>
    public string? EventType { get; set; }

    /// <summary>A chave de partição: garante a ordem por comerciante no Kafka (ADR-0002).</summary>
    public string? PartitionKey { get; set; }
}
