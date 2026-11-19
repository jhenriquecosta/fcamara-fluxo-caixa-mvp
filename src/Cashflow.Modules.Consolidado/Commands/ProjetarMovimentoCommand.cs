using Cashflow.Application;
using Cashflow.Application.Decorators.DatabaseRetry;
using Cashflow.CrossCuttingConcerns.Security;
using Cashflow.Modules.Consolidado.Entities;
using Cashflow.Modules.Consolidado.Repositories;
using Microsoft.Extensions.Logging;

namespace Cashflow.Modules.Consolidado.Commands;

/// <summary>Somar no consolidado do dia um movimento que chegou do barramento.</summary>
public sealed class ProjetarMovimentoCommand : ICommand
{
    /// <summary>O movimento a somar.</summary>
    public required MovimentoDoDia Movimento { get; init; }
}

/// <summary>
/// Projeta o movimento numa transação. É idempotente pelo <c>eventId</c>, então pode ser repetido em
/// falha transitória do banco e pode receber o mesmo evento duas vezes do Kafka sem somar em dobro.
/// </summary>
[DatabaseRetry]
public sealed class ProjetarMovimentoCommandHandler(
    IConsolidadoRepository consolidadoRepository,
    ILogger<ProjetarMovimentoCommandHandler> logger) : ICommandHandler<ProjetarMovimentoCommand>
{
    /// <inheritdoc />
    public async Task HandleAsync(ProjetarMovimentoCommand command, CancellationToken cancellationToken = default)
    {
        var movimento = command.Movimento;
        var comercianteNoLog = PseudonimoDeLog.DoComerciante(movimento.ComercianteId);

        ResultadoDaProjecao resultado;
        using (await consolidadoRepository.UnitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken))
        {
            resultado = await consolidadoRepository.ProjetarAsync(movimento, cancellationToken);
            await consolidadoRepository.UnitOfWork.CommitTransactionAsync(cancellationToken);
        }

        if (resultado == ResultadoDaProjecao.EventoJaProcessado)
        {
            logger.LogInformation("evento repetido ignorado {EventId} {ComercianteHash}",
                movimento.EventId, comercianteNoLog);
            return;
        }

        logger.LogInformation("evento aplicado {EventId} {ComercianteHash} {Data} {Delta}",
            movimento.EventId, comercianteNoLog, movimento.Data, movimento.EfeitoNoSaldo);
    }
}
