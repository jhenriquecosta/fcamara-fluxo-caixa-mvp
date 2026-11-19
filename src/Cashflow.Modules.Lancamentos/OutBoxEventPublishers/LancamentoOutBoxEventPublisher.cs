using Cashflow.Contracts;
using Cashflow.Domain.Infrastructure.MessageBrokers;
using Cashflow.Modules.Lancamentos.Commands;
using Cashflow.Modules.Lancamentos.Constants;

namespace Cashflow.Modules.Lancamentos.OutBoxEventPublishers;

/// <summary>Leva o evento do outbox ao barramento, com o comerciante como chave de partição (ordem por comerciante).</summary>
public sealed class LancamentoOutBoxEventPublisher(IMessageBus messageBus) : IOutBoxEventPublisher
{
    public static string[] CanHandleEventTypes() =>
        [EventTypeConstants.LancamentoRegistrado, EventTypeConstants.LancamentoEstornado];

    public static string CanHandleEventSource() => typeof(PublishEventsCommand).Assembly.GetName().Name!;

    public async Task HandleAsync(PublishingOutBoxEvent outbox, CancellationToken cancellationToken = default)
    {
        var envelope = CashflowEventSerializer.Deserialize<LancamentoEventPayload>(outbox.Payload)
                       ?? throw new InvalidOperationException($"evento do outbox inválido: {outbox.Id}");

        await messageBus.SendAsync(envelope.Payload, new MetaData
        {
            MessageId = envelope.EventId.ToString(),
            MessageVersion = envelope.Version.ToString(),
            EventType = envelope.EventType,
            CreationDateTime = envelope.OccurredAt,
            PartitionKey = envelope.Payload.ComercianteId.ToString()
        }, cancellationToken);
    }
}
