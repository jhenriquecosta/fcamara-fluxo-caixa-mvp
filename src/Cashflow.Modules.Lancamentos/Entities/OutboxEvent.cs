using Cashflow.Domain.Entities;

namespace Cashflow.Modules.Lancamentos.Entities;

/// <summary>
/// O evento de integração gravado na mesma transação do lançamento (padrão Transactional Outbox,
/// ADR-0001). Se o lançamento foi gravado, o evento também foi; o publicador o leva ao Kafka depois,
/// e uma queda do broker não perde nada.
/// </summary>
/// <remarks>O <see cref="Entity{TKey}.Id"/> é também o <c>eventId</c> do envelope: é por ele que o consolidado descarta repetições.</remarks>
public sealed class OutboxEvent : Entity<Guid>, IAggregateRoot
{
    /// <summary>O tipo do evento (<c>lancamento.registrado</c> ou <c>lancamento.estornado</c>).</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>O id do lançamento que originou o evento.</summary>
    public string ObjectId { get; set; } = string.Empty;

    /// <summary>O envelope <c>CashflowEvent</c> já serializado, exatamente como será publicado.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Se o evento já chegou ao barramento.</summary>
    public bool Published { get; set; }
}
