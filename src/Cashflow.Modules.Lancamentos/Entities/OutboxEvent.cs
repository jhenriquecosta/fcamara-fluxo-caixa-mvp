using Cashflow.Domain.Entities;

namespace Cashflow.Modules.Lancamentos.Entities;

/// <summary>
/// O evento gravado na mesma transação do lançamento (outbox, ADR-0001). O <see cref="Entity{TKey}.Id"/>
/// é também o <c>eventId</c> do envelope: é por ele que o consolidado descarta repetições.
/// </summary>
public sealed class OutboxEvent : Entity<Guid>, IAggregateRoot
{
    public string EventType { get; set; } = string.Empty;

    public string ObjectId { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool Published { get; set; }
}
