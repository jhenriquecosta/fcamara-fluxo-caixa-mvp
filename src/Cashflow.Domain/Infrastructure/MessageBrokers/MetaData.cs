namespace Cashflow.Domain.Infrastructure.MessageBrokers;

/// <summary>O que viaja com a mensagem além do payload: identidade, versão, tipo e partição.</summary>
public sealed class MetaData
{
    /// <summary>O id da mensagem. No consumidor do consolidado é a chave que descarta repetições.</summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>A versão do schema do payload. Muda quando o contrato muda de forma incompatível.</summary>
    public string MessageVersion { get; set; } = "1";

    /// <summary>A correlação com a requisição de origem, para seguir o pedido nos logs dos dois lados.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>Quando o fato aconteceu (o <c>occurredAt</c> do envelope).</summary>
    public DateTimeOffset? CreationDateTime { get; set; }

    /// <summary>Quando a mensagem entrou no broker, se ele informar.</summary>
    public DateTimeOffset? EnqueuedDateTime { get; set; }

    /// <summary>O tipo do evento no barramento (<c>lancamento.registrado</c>, ...). Acréscimo à referência:
    /// o envelope publicado carrega o tipo, e um mesmo payload serve a mais de um evento.</summary>
    public string? EventType { get; set; }

    /// <summary>A chave de partição: garante a ordem por comerciante no Kafka (ADR-0002).</summary>
    public string? PartitionKey { get; set; }
}
