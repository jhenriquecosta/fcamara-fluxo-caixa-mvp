using Cashflow.Domain.Entities;

namespace Cashflow.Domain.Events;

/// <summary>
/// Uma raiz de agregado acabou de ser criada. O módulo de Lançamentos o despacha a cada lançamento
/// ou estorno, e o handler dele grava o evento de integração no outbox.
/// </summary>
/// <typeparam name="T">A raiz criada.</typeparam>
public sealed class EntityCreatedEvent<T>(T entity, DateTimeOffset eventDateTime) : IDomainEvent
    where T : IAggregateRoot
{
    /// <summary>A raiz recém-criada, já com identidade.</summary>
    public T Entity { get; } = entity;

    /// <summary>O instante do fato, que vira o <c>occurredAt</c> do evento publicado.</summary>
    public DateTimeOffset EventDateTime { get; } = eventDateTime;
}
