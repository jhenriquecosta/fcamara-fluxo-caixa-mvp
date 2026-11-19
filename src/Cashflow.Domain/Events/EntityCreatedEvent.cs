using Cashflow.Domain.Entities;

namespace Cashflow.Domain.Events;

public sealed class EntityCreatedEvent<T>(T entity, DateTimeOffset eventDateTime) : IDomainEvent
    where T : IAggregateRoot
{
    public T Entity { get; } = entity;

    public DateTimeOffset EventDateTime { get; } = eventDateTime;
}
