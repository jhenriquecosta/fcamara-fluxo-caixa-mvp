using Cashflow.Application;
using Cashflow.CrossCuttingConcerns.DateTimes;
using Cashflow.Domain.Infrastructure.MessageBrokers;
using Cashflow.Modules.Lancamentos.Observability;
using Cashflow.Modules.Lancamentos.Repositories;

namespace Cashflow.Modules.Lancamentos.Commands;

public sealed class PublishEventsCommand : ICommand
{
    public int SentEventsCount { get; set; }
}

public sealed class PublishEventsCommandHandler(
    IOutboxEventRepository outboxEventRepository,
    IMessageBus messageBus,
    IDateTimeProvider dateTimeProvider,
    LancamentosMetrics metrics) : ICommandHandler<PublishEventsCommand>
{
    private const int Lote = 100;

    public async Task HandleAsync(PublishEventsCommand command, CancellationToken cancellationToken = default)
    {
        using (await outboxEventRepository.UnitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken))
        {
            var eventos = await outboxEventRepository.GetPendentesAsync(Lote, cancellationToken);

            foreach (var evento in eventos)
            {
                await messageBus.SendAsync(new PublishingOutBoxEvent
                {
                    Id = evento.Id.ToString(),
                    EventType = evento.EventType,
                    EventSource = typeof(PublishEventsCommand).Assembly.GetName().Name!,
                    Payload = evento.Message
                }, cancellationToken);

                await outboxEventRepository.MarcarPublicadoAsync(evento.Id, dateTimeProvider.OffsetUtcNow, cancellationToken);
            }

            await outboxEventRepository.UnitOfWork.CommitTransactionAsync(cancellationToken);
            command.SentEventsCount = eventos.Count;
        }

        metrics.OutboxAge(await outboxEventRepository.IdadeDoPendenteMaisAntigoAsync(cancellationToken));
    }
}
