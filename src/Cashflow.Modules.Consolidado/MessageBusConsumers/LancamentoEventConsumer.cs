using Cashflow.Application;
using Cashflow.Contracts;
using Cashflow.Domain.Infrastructure.MessageBrokers;
using Cashflow.Modules.Consolidado.Commands;
using Cashflow.Modules.Consolidado.Entities;

namespace Cashflow.Modules.Consolidado.MessageBusConsumers;

/// <summary>
/// A entrada do consolidado no barramento: converte o evento de lançamento no comando de projeção e o
/// despacha. O <c>MessageId</c> é o <c>eventId</c> do envelope, e é ele que torna a projeção idempotente.
/// </summary>
public sealed class LancamentoEventConsumer(Dispatcher dispatcher)
    : IMessageBusConsumer<LancamentoEventConsumer, LancamentoEventPayload>
{
    /// <inheritdoc />
    public Task HandleAsync(LancamentoEventPayload data, MetaData metaData, CancellationToken cancellationToken = default) =>
        dispatcher.DispatchAsync(new ProjetarMovimentoCommand
        {
            Movimento = new MovimentoDoDia(
                Guid.Parse(metaData.MessageId), data.ComercianteId, data.Tipo, data.Valor, data.Data)
        }, cancellationToken);
}
