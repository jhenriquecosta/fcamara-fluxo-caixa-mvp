namespace Cashflow.Domain.Infrastructure.MessageBrokers;

public interface IOutBoxEventPublisher
{
    static abstract string[] CanHandleEventTypes();

    static abstract string CanHandleEventSource();

    Task HandleAsync(PublishingOutBoxEvent outbox, CancellationToken cancellationToken = default);
}

public sealed class PublishingOutBoxEvent
{
    public required string Id { get; init; }

    public required string EventType { get; init; }

    public required string EventSource { get; init; }

    public required string Payload { get; init; }
}
