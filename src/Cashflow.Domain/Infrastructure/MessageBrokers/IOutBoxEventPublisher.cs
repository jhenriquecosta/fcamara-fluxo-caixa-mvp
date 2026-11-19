namespace Cashflow.Domain.Infrastructure.MessageBrokers;

/// <summary>
/// Sabe levar ao barramento um tipo de evento gravado no outbox de um módulo. O
/// <see cref="MessageBus"/> escolhe o publicador pela origem (o assembly do módulo) e pelo tipo do evento.
/// </summary>
public interface IOutBoxEventPublisher
{
    /// <summary>Os tipos de evento (<c>lancamento.registrado</c>, ...) que este publicador atende.</summary>
    static abstract string[] CanHandleEventTypes();

    /// <summary>A origem atendida: o nome do assembly do módulo que gravou o evento.</summary>
    static abstract string CanHandleEventSource();

    /// <summary>Publica o evento. Lançar deixa o evento pendente no outbox para a próxima volta do publicador.</summary>
    Task HandleAsync(PublishingOutBoxEvent outbox, CancellationToken cancellationToken = default);
}

/// <summary>Um evento lido do outbox a caminho do barramento.</summary>
public sealed class PublishingOutBoxEvent
{
    /// <summary>O id do evento, que é também o <c>eventId</c> do envelope e a chave de deduplicação do consumidor.</summary>
    public required string Id { get; init; }

    /// <summary>O tipo do evento (<c>lancamento.registrado</c> ou <c>lancamento.estornado</c>).</summary>
    public required string EventType { get; init; }

    /// <summary>O assembly do módulo que gravou o evento.</summary>
    public required string EventSource { get; init; }

    /// <summary>O envelope serializado, exatamente como foi gravado na transação do lançamento.</summary>
    public required string Payload { get; init; }
}
