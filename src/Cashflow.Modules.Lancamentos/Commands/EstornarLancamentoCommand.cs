using System.Text.Json;
using Cashflow.Application;
using Cashflow.Application.Decorators.AuditLog;
using Cashflow.Contracts;
using Cashflow.CrossCuttingConcerns.DateTimes;
using Cashflow.CrossCuttingConcerns.Security;
using Cashflow.Domain.Events;
using Cashflow.Modules.Lancamentos.Entities;
using Cashflow.Modules.Lancamentos.Models;
using Cashflow.Modules.Lancamentos.Repositories;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Cashflow.Modules.Lancamentos.Commands;

public sealed class EstornarLancamentoCommand : ICommand
{
    public required Guid ComercianteId { get; init; }

    public required Guid LancamentoId { get; init; }

    public required string IdempotencyKey { get; init; }

    public RegistroResultado? Resultado { get; set; }
}

[AuditLog]
public sealed class EstornarLancamentoCommandHandler(
    ILancamentoRepository lancamentoRepository,
    IIdempotenciaRepository idempotenciaRepository,
    Dispatcher dispatcher,
    IDateTimeProvider dateTimeProvider,
    TimeZoneInfo fusoDoNegocio,
    ILogger<EstornarLancamentoCommandHandler> logger) : ICommandHandler<EstornarLancamentoCommand>
{
    private const string ViolacaoDeUnicidade = "23505";

    public async Task HandleAsync(EstornarLancamentoCommand command, CancellationToken cancellationToken = default)
    {
        var comercianteHash = Hashes.Comerciante(command.ComercianteId);
        var reivindicacao = await idempotenciaRepository.ReivindicarAsync(command.ComercianteId, command.IdempotencyKey,
            command.LancamentoId.ToString(), cancellationToken);

        if (Idempotencia.Desfecho(reivindicacao, logger, comercianteHash, command.IdempotencyKey) is { } desfecho)
        {
            command.Resultado = desfecho;
            return;
        }

        RegistroResultado resultado;
        try
        {
            resultado = await EstornarAsync(command, cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == ViolacaoDeUnicidade)
        {
            // outro estorno do mesmo lançamento venceu a corrida: o índice único é a última barreira
            resultado = new RegistroResultado.Conflito();
        }
        catch (Exception ex)
        {
            await idempotenciaRepository.FalharAsync(command.ComercianteId, command.IdempotencyKey, CancellationToken.None);
            logger.LogError(ex, "falha ao estornar lançamento {EstornoDe} {ComercianteHash}",
                command.LancamentoId, comercianteHash);
            throw;
        }

        if (resultado is RegistroResultado.Criado criado)
        {
            logger.LogInformation("estorno registrado {LancamentoId} {EstornoDe} {ComercianteHash}",
                criado.Lancamento.Id, command.LancamentoId, comercianteHash);
        }
        else
        {
            await idempotenciaRepository.FalharAsync(command.ComercianteId, command.IdempotencyKey, cancellationToken);
        }

        command.Resultado = resultado;
    }

    private async Task<RegistroResultado> EstornarAsync(EstornarLancamentoCommand command, CancellationToken ct)
    {
        using var transacao = await lancamentoRepository.UnitOfWork.BeginTransactionAsync(cancellationToken: ct);

        var original = await lancamentoRepository.GetForUpdateAsync(command.ComercianteId, command.LancamentoId, ct);
        if (original is null)
            return new RegistroResultado.NaoEncontrado();

        if (original.EhEstorno)
            return new RegistroResultado.RegraInvalida();

        if (await lancamentoRepository.ExisteEstornoDeAsync(original.Id, ct))
            return new RegistroResultado.Conflito();

        var agora = dateTimeProvider.OffsetUtcNow;
        var estorno = Lancamento.Estornar(original, DiaDeNegocio.De(agora, fusoDoNegocio), agora);
        var modelo = LancamentoModel.De(estorno);

        await lancamentoRepository.AddAsync(estorno, ct);
        await dispatcher.DispatchAsync(new EntityCreatedEvent<Lancamento>(estorno, agora), ct);
        await idempotenciaRepository.ConcluirAsync(command.ComercianteId, command.IdempotencyKey, estorno.Id,
            JsonSerializer.Serialize(modelo, CashflowJson.Options), ct);
        await lancamentoRepository.UnitOfWork.CommitTransactionAsync(ct);

        return new RegistroResultado.Criado(modelo);
    }
}
