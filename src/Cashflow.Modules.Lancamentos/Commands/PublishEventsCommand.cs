using Cashflow.Application;
using Cashflow.CrossCuttingConcerns.DateTimes;
using Cashflow.Domain.Infrastructure.MessageBrokers;
using Cashflow.Modules.Lancamentos.Observability;
using Cashflow.Modules.Lancamentos.Repositories;

namespace Cashflow.Modules.Lancamentos.Commands;

/// <summary>Publicar no barramento um lote de eventos pendentes do outbox. Despachado em laço pelo <c>PublishEventWorker</c>.</summary>
public sealed class PublishEventsCommand : ICommand
{
    /// <summary>Quantos eventos este lote publicou. Zero diz ao worker que pode esperar antes da próxima volta.</summary>
    public int SentEventsCount { get; set; }
}

/// <summary>
/// Lê até <see cref="TamanhoDoLote"/> eventos pendentes, publica cada um e o marca como publicado, numa
/// transação. Se a publicação falhar, a transação desfaz e os eventos continuam pendentes para a próxima volta.
/// </summary>
/// <remarks>
/// Entrega at-least-once: uma queda entre publicar e confirmar republica o lote, e o consumidor
/// descarta a repetição pelo <c>eventId</c>. Vários publicadores podem rodar juntos: o <c>SKIP LOCKED</c>
/// impede que dois peguem o mesmo evento.
/// </remarks>
public sealed class PublishEventsCommandHandler(
    IOutboxEventRepository outboxEventRepository,
    IMessageBus messageBus,
    IDateTimeProvider dateTimeProvider,
    LancamentosMetrics metrics) : ICommandHandler<PublishEventsCommand>
{
    private const int TamanhoDoLote = 100;

    /// <inheritdoc />
    public async Task HandleAsync(PublishEventsCommand command, CancellationToken cancellationToken = default)
    {
        using (await outboxEventRepository.UnitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken))
        {
            var pendentes = await outboxEventRepository.GetPendentesAsync(TamanhoDoLote, cancellationToken);

            foreach (var evento in pendentes)
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
            command.SentEventsCount = pendentes.Count;
        }

        metrics.RegistrarIdadeDoPendenteMaisAntigo(await outboxEventRepository.IdadeDoPendenteMaisAntigoAsync(cancellationToken));
    }
}
