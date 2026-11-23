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

/// <summary>Corrigir um lançamento com um contra-lançamento de tipo oposto (RF-02). O original não muda.</summary>
public sealed class EstornarLancamentoCommand : ICommand
{
    /// <summary>O dono do caixa, tirado do token. Só estorna lançamento dele.</summary>
    public required Guid ComercianteId { get; init; }

    /// <summary>O lançamento a estornar.</summary>
    public required Guid LancamentoId { get; init; }

    /// <summary>O <c>Idempotency-Key</c> enviado pelo cliente.</summary>
    public required string IdempotencyKey { get; init; }

    /// <summary>Preenchido pelo handler.</summary>
    public ResultadoDoLancamento? Resultado { get; set; }
}

/// <summary>
/// Estorna com a mesma garantia do registro (estorno e evento numa transação, chave de idempotência) e
/// mais três regras: o lançamento tem de existir e ser do comerciante, não pode ser um estorno, e só
/// pode ser estornado uma vez.
/// </summary>
/// <remarks>
/// "Uma vez só" tem duas barreiras: a leitura com <c>FOR UPDATE</c> serializa estornos simultâneos do
/// mesmo lançamento, e o índice único em <c>estorno_de</c> recusa o que ainda escapar.
/// </remarks>
[AuditLog]
public sealed class EstornarLancamentoCommandHandler(
    ILancamentoRepository lancamentoRepository,
    IIdempotenciaRepository idempotenciaRepository,
    Dispatcher dispatcher,
    IDateTimeProvider dateTimeProvider,
    TimeZoneInfo fusoDoNegocio,
    ILogger<EstornarLancamentoCommandHandler> logger) : ICommandHandler<EstornarLancamentoCommand>
{
    private const string PostgresViolacaoDeUnicidade = "23505";

    /// <inheritdoc />
    public async Task HandleAsync(EstornarLancamentoCommand command, CancellationToken cancellationToken = default)
    {
        var comercianteNoLog = PseudonimoDeLog.DoComerciante(command.ComercianteId);
        var reserva = await idempotenciaRepository.ReservarAsync(command.ComercianteId, command.IdempotencyKey,
            command.LancamentoId.ToString(), cancellationToken);

        if (ChaveDeIdempotencia.RespostaSeJaUsada(reserva, logger, comercianteNoLog, command.IdempotencyKey) is { } jaRespondido)
        {
            command.Resultado = jaRespondido;
            return;
        }

        ResultadoDoLancamento resultado;
        try
        {
            resultado = await EstornarNaTransacaoAsync(command, cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresViolacaoDeUnicidade)
        {
            // outro estorno do mesmo lançamento venceu a corrida: o índice único é a última barreira
            resultado = new ResultadoDoLancamento.JaEstornado();
        }
        catch (Exception ex)
        {
            await idempotenciaRepository.MarcarFalhaAsync(command.ComercianteId, command.IdempotencyKey, CancellationToken.None);
            logger.LogError(ex, "falha ao estornar lançamento {EstornoDe} {ComercianteHash}",
                command.LancamentoId, comercianteNoLog);
            throw;
        }

        if (resultado is ResultadoDoLancamento.Criado criado)
        {
            logger.LogInformation("estorno registrado {LancamentoId} {EstornoDe} {ComercianteHash}",
                criado.Lancamento.Id, command.LancamentoId, comercianteNoLog);
        }
        else
        {
            // recusa de regra: a chave fica livre para o cliente repetir este mesmo estorno quando a regra deixar
            // (a impressão do pedido, que no estorno é o lancamentoId, segue amarrada a ela: outro id com a mesma chave é recusado)
            await idempotenciaRepository.MarcarFalhaAsync(command.ComercianteId, command.IdempotencyKey, cancellationToken);
        }

        command.Resultado = resultado;
    }

    private async Task<ResultadoDoLancamento> EstornarNaTransacaoAsync(EstornarLancamentoCommand command, CancellationToken ct)
    {
        using var transacao = await lancamentoRepository.UnitOfWork.BeginTransactionAsync(cancellationToken: ct);

        var original = await lancamentoRepository.GetForUpdateAsync(command.ComercianteId, command.LancamentoId, ct);
        if (original is null)
            return new ResultadoDoLancamento.LancamentoNaoEncontrado();

        if (original.EhEstorno)
            return new ResultadoDoLancamento.EstornoDeEstorno();

        if (await lancamentoRepository.ExisteEstornoDeAsync(original.Id, ct))
            return new ResultadoDoLancamento.JaEstornado();

        var agora = dateTimeProvider.OffsetUtcNow;
        var estorno = Lancamento.Estornar(original, DiaDeNegocio.De(agora, fusoDoNegocio), agora);
        var resposta = LancamentoModel.De(estorno);

        await lancamentoRepository.AddAsync(estorno, ct);
        await dispatcher.DispatchAsync(new EntityCreatedEvent<Lancamento>(estorno, agora), ct);
        await idempotenciaRepository.ConcluirAsync(command.ComercianteId, command.IdempotencyKey, estorno.Id,
            JsonSerializer.Serialize(resposta, CashflowJson.Options), ct);
        await lancamentoRepository.UnitOfWork.CommitTransactionAsync(ct);

        return new ResultadoDoLancamento.Criado(resposta);
    }
}
