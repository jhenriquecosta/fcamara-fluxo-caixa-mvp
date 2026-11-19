using Cashflow.Application;
using Cashflow.Application.Decorators.DatabaseRetry;
using Cashflow.CrossCuttingConcerns.Security;
using Cashflow.Modules.Consolidado.Entities;
using Cashflow.Modules.Consolidado.Repositories;
using Microsoft.Extensions.Logging;

namespace Cashflow.Modules.Consolidado.Commands;

public sealed class ProjetarMovimentoCommand : ICommand
{
    public required MovimentoDoDia Movimento { get; init; }
}

[DatabaseRetry]
public sealed class ProjetarMovimentoCommandHandler(
    IConsolidadoRepository consolidadoRepository,
    ILogger<ProjetarMovimentoCommandHandler> logger) : ICommandHandler<ProjetarMovimentoCommand>
{
    public async Task HandleAsync(ProjetarMovimentoCommand command, CancellationToken cancellationToken = default)
    {
        var movimento = command.Movimento;
        var comercianteHash = Hashes.Comerciante(movimento.ComercianteId);

        ProjecaoResultado resultado;
        using (await consolidadoRepository.UnitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken))
        {
            resultado = await consolidadoRepository.AplicarAsync(movimento, cancellationToken);
            await consolidadoRepository.UnitOfWork.CommitTransactionAsync(cancellationToken);
        }

        if (resultado == ProjecaoResultado.Duplicado)
        {
            logger.LogInformation("evento repetido ignorado {EventId} {ComercianteHash}",
                movimento.EventId, comercianteHash);
            return;
        }

        logger.LogInformation("evento aplicado {EventId} {ComercianteHash} {Data} {Delta}",
            movimento.EventId, comercianteHash, movimento.Data, movimento.Delta);
    }
}
