using Cashflow.Contracts;
using Cashflow.Domain.Events;
using Cashflow.Modules.Lancamentos.Constants;
using Cashflow.Modules.Lancamentos.Entities;
using Cashflow.Modules.Lancamentos.Repositories;

namespace Cashflow.Modules.Lancamentos.EventHandlers;

/// <summary>
/// O outbox nasce do evento de domínio, como no <c>ProductCreatedEventHandler</c> da referência.
/// Roda dentro da transação do comando: o lançamento e o evento são gravados juntos ou nenhum dos dois.
/// </summary>
public sealed class LancamentoCreatedEventHandler(IOutboxEventRepository outboxEventRepository)
    : IDomainEventHandler<EntityCreatedEvent<Lancamento>>
{
    public async Task HandleAsync(EntityCreatedEvent<Lancamento> domainEvent, CancellationToken cancellationToken = default)
    {
        var lancamento = domainEvent.Entity;
        var eventId = Guid.NewGuid();
        var eventType = lancamento.EhEstorno
            ? EventTypeConstants.LancamentoEstornado
            : EventTypeConstants.LancamentoRegistrado;

        var envelope = new CashflowEvent<LancamentoEventPayload>(
            eventId, eventType, domainEvent.EventDateTime, 1,
            new LancamentoEventPayload(
                lancamento.Id, lancamento.ComercianteId, lancamento.Tipo.Texto(),
                lancamento.Valor, lancamento.Data, lancamento.EstornoDe));

        await outboxEventRepository.AddAsync(new OutboxEvent
        {
            Id = eventId,
            EventType = eventType,
            ObjectId = lancamento.Id.ToString(),
            Message = CashflowEventSerializer.Serialize(envelope),
            Published = false,
            CreatedDateTime = domainEvent.EventDateTime
        }, cancellationToken);
    }
}
