namespace Cashflow.Domain.Infrastructure.MessageBrokers;

public interface IMessageBus
{
    Task SendAsync<T>(T message, MetaData? metaData = null, CancellationToken cancellationToken = default);

    Task ReceiveAsync<TConsumer, T>(Func<T, MetaData, Task> action, CancellationToken cancellationToken = default);

    Task ReceiveAsync<TConsumer, T>(CancellationToken cancellationToken = default);

    Task SendAsync(PublishingOutBoxEvent outbox, CancellationToken cancellationToken = default);
}
