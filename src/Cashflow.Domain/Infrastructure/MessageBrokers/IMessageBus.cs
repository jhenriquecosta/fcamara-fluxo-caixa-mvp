namespace Cashflow.Domain.Infrastructure.MessageBrokers;

public sealed record MetaData(
    Guid MessageId,
    DateTimeOffset CreatedDateTime,
    string EventType,
    int Version = 1);

public sealed record Message<T>(MetaData MetaData, T Payload);

public interface IMessageBus
{
    Task SendAsync<T>(Message<T> message, CancellationToken cancellationToken = default);
}

public interface IMessageBusConsumer<T>
{
    Task HandleAsync(Message<T> message, CancellationToken cancellationToken = default);
}

public interface IOutBoxEventPublisher
{
    Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default) where T : class;
}
