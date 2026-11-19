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

public sealed class RegistrarLancamentoCommand : ICommand
{
    public required Guid ComercianteId { get; init; }

    public required TipoLancamento Tipo { get; init; }

    public required decimal Valor { get; init; }

    public string? Descricao { get; init; }

    public required string IdempotencyKey { get; init; }

    /// <summary>Preenchido pelo handler, como o <c>SentEventsCount</c> do <c>PublishEventsCommand</c> da referência.</summary>
    public RegistroResultado? Resultado { get; set; }

    /// <summary>O hash do corpo que a chave de idempotência protege: mesma chave com outro corpo é 422.</summary>
    public static string HashDoCorpo(TipoLancamento tipo, decimal valor, string? descricao) =>
        Hashes.Texto($"{valor.ToString("0.00", CultureInfo.InvariantCulture)}|{tipo.Texto()}|{descricao ?? ""}");
}

[AuditLog]
public sealed class RegistrarLancamentoCommandHandler(
    ILancamentoRepository lancamentoRepository,
    IIdempotenciaRepository idempotenciaRepository,
    Dispatcher dispatcher,
    IDateTimeProvider dateTimeProvider,
    TimeZoneInfo fusoDoNegocio,
    ILogger<RegistrarLancamentoCommandHandler> logger) : ICommandHandler<RegistrarLancamentoCommand>
{
    public async Task HandleAsync(RegistrarLancamentoCommand command, CancellationToken cancellationToken = default)
    {
        var comercianteHash = Hashes.Comerciante(command.ComercianteId);
        var reivindicacao = await idempotenciaRepository.ReivindicarAsync(command.ComercianteId, command.IdempotencyKey,
            RegistrarLancamentoCommand.HashDoCorpo(command.Tipo, command.Valor, command.Descricao), cancellationToken);

        if (Idempotencia.Desfecho(reivindicacao, logger, comercianteHash, command.IdempotencyKey) is { } desfecho)
        {
            command.Resultado = desfecho;
            return;
        }

        var agora = dateTimeProvider.OffsetUtcNow;
        var lancamento = Lancamento.Registrar(command.ComercianteId, command.Tipo, command.Valor,
            command.Descricao, DiaDeNegocio.De(agora, fusoDoNegocio), agora);
        var modelo = LancamentoModel.De(lancamento);

        try
        {
            using (await lancamentoRepository.UnitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken))
            {
                await lancamentoRepository.AddAsync(lancamento, cancellationToken);
                await dispatcher.DispatchAsync(new EntityCreatedEvent<Lancamento>(lancamento, agora), cancellationToken);
                await idempotenciaRepository.ConcluirAsync(command.ComercianteId, command.IdempotencyKey, lancamento.Id,
                    JsonSerializer.Serialize(modelo, CashflowJson.Options), cancellationToken);
                await lancamentoRepository.UnitOfWork.CommitTransactionAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            await idempotenciaRepository.FalharAsync(command.ComercianteId, command.IdempotencyKey, CancellationToken.None);
            logger.LogError(ex, "falha ao registrar lançamento {ComercianteHash}", comercianteHash);
            throw;
        }

        logger.LogInformation("lançamento registrado {LancamentoId} {ComercianteHash} {Tipo} {Valor}",
            lancamento.Id, comercianteHash, lancamento.Tipo.Texto(), lancamento.Valor);
        command.Resultado = new RegistroResultado.Criado(modelo);
    }
}
