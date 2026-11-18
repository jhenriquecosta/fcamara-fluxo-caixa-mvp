using Cashflow.Application;
using Cashflow.Contracts;
using Cashflow.Domain.Infrastructure.MessageBrokers;
using Cashflow.Modules.Consolidado.Commands;
using Cashflow.Modules.Consolidado.Entities;

namespace Cashflow.Modules.Consolidado.MessageBusConsumers;

/// <summary>
/// Recebe o evento de lançamento do barramento e o converte no comando de projeção. O
/// <c>MessageId</c> é o <c>eventId</c> do envelope: é ele que torna a projeção idempotente.
/// </summary>
public sealed class LancamentoEventConsumer(Dispatcher dispatcher)
    : IMessageBusConsumer<LancamentoEventConsumer, LancamentoEventPayload>
{
    public Task HandleAsync(LancamentoEventPayload data, MetaData metaData, CancellationToken cancellationToken = default) =>
        dispatcher.DispatchAsync(new ProjetarMovimentoCommand
        {
            Movimento = new MovimentoDoDia(
                Guid.Parse(metaData.MessageId), data.ComercianteId, data.Tipo, data.Valor, data.Data)
        }, cancellationToken);
}
