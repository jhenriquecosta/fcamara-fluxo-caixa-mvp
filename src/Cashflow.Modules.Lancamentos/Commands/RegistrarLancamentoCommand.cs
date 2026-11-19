using System.Globalization;
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

namespace Cashflow.Modules.Lancamentos.Commands;

/// <summary>Registrar um crédito ou débito no caixa do comerciante (RF-01).</summary>
public sealed class RegistrarLancamentoCommand : ICommand
{
    /// <summary>O dono do caixa, tirado do token.</summary>
    public required Guid ComercianteId { get; init; }

    /// <summary>Crédito ou débito.</summary>
    public required TipoLancamento Tipo { get; init; }

    /// <summary>O valor, maior que zero.</summary>
    public required decimal Valor { get; init; }

    /// <summary>Texto livre, opcional.</summary>
    public string? Descricao { get; init; }

    /// <summary>O <c>Idempotency-Key</c> enviado pelo cliente.</summary>
    public required string IdempotencyKey { get; init; }

    /// <summary>Preenchido pelo handler, como o <c>SentEventsCount</c> do <c>PublishEventsCommand</c> da referência.</summary>
    public ResultadoDoLancamento? Resultado { get; set; }

    /// <summary>
    /// A impressão digital do corpo que a chave de idempotência protege. Mesma chave com outro corpo é 422;
    /// o valor é normalizado em duas casas para que <c>100</c> e <c>100.00</c> sejam o mesmo pedido.
    /// </summary>
    public static string ImpressaoDigitalDoCorpo(TipoLancamento tipo, decimal valor, string? descricao) =>
        PseudonimoDeLog.ImpressaoDigital(
            $"{valor.ToString("0.00", CultureInfo.InvariantCulture)}|{tipo.NoContrato()}|{descricao ?? ""}");
}

/// <summary>
/// Registra o lançamento e o seu evento de integração numa transação só. A ordem importa:
/// 1) reserva a chave de idempotência (fora da transação, para a concorrente enxergar);
/// 2) grava o lançamento, o evento do outbox e a resposta da chave juntos;
/// 3) em qualquer falha, libera a chave para o cliente tentar de novo.
/// </summary>
[AuditLog]
public sealed class RegistrarLancamentoCommandHandler(
    ILancamentoRepository lancamentoRepository,
    IIdempotenciaRepository idempotenciaRepository,
    Dispatcher dispatcher,
    IDateTimeProvider dateTimeProvider,
    TimeZoneInfo fusoDoNegocio,
    ILogger<RegistrarLancamentoCommandHandler> logger) : ICommandHandler<RegistrarLancamentoCommand>
{
    /// <inheritdoc />
    public async Task HandleAsync(RegistrarLancamentoCommand command, CancellationToken cancellationToken = default)
    {
        var comercianteNoLog = PseudonimoDeLog.DoComerciante(command.ComercianteId);
        var reserva = await idempotenciaRepository.ReservarAsync(command.ComercianteId, command.IdempotencyKey,
            RegistrarLancamentoCommand.ImpressaoDigitalDoCorpo(command.Tipo, command.Valor, command.Descricao),
            cancellationToken);

        if (ChaveDeIdempotencia.RespostaSeJaUsada(reserva, logger, comercianteNoLog, command.IdempotencyKey) is { } jaRespondido)
        {
            command.Resultado = jaRespondido;
            return;
        }

        var agora = dateTimeProvider.OffsetUtcNow;
        var lancamento = Lancamento.Registrar(command.ComercianteId, command.Tipo, command.Valor,
            command.Descricao, DiaDeNegocio.De(agora, fusoDoNegocio), agora);
        var resposta = LancamentoModel.De(lancamento);

        try
        {
            using (await lancamentoRepository.UnitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken))
            {
                await lancamentoRepository.AddAsync(lancamento, cancellationToken);
                await dispatcher.DispatchAsync(new EntityCreatedEvent<Lancamento>(lancamento, agora), cancellationToken);
                await idempotenciaRepository.ConcluirAsync(command.ComercianteId, command.IdempotencyKey, lancamento.Id,
                    JsonSerializer.Serialize(resposta, CashflowJson.Options), cancellationToken);
                await lancamentoRepository.UnitOfWork.CommitTransactionAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            await idempotenciaRepository.MarcarFalhaAsync(command.ComercianteId, command.IdempotencyKey, CancellationToken.None);
            logger.LogError(ex, "falha ao registrar lançamento {ComercianteHash}", comercianteNoLog);
            throw;
        }

        logger.LogInformation("lançamento registrado {LancamentoId} {ComercianteHash} {Tipo} {Valor}",
            lancamento.Id, comercianteNoLog, lancamento.Tipo.NoContrato(), lancamento.Valor);
        command.Resultado = new ResultadoDoLancamento.Criado(resposta);
    }
}
