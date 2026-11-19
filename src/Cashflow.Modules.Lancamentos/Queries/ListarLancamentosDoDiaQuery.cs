using Cashflow.Application;
using Cashflow.Application.Decorators.AuditLog;
using Cashflow.Application.Decorators.DatabaseRetry;
using Cashflow.CrossCuttingConcerns.DateTimes;
using Cashflow.Modules.Lancamentos.Entities;
using Cashflow.Modules.Lancamentos.Models;
using Cashflow.Modules.Lancamentos.Repositories;

namespace Cashflow.Modules.Lancamentos.Queries;

/// <summary>Os lançamentos de um dia do comerciante, para a tela listar e estornar (PRD-0003 RF10).</summary>
public sealed class ListarLancamentosDoDiaQuery : IQuery<IReadOnlyList<LancamentoDoDiaModel>>
{
    /// <summary>O comerciante do token: nunca se lista o caixa de outro.</summary>
    public required Guid ComercianteId { get; init; }

    /// <summary>O dia de negócio; nulo quer dizer hoje em America/Sao_Paulo.</summary>
    public DateOnly? Data { get; init; }
}

/// <summary>Resolve o dia de negócio quando a consulta não diz e lê do repositório.</summary>
[AuditLog]
[DatabaseRetry]
public sealed class ListarLancamentosDoDiaQueryHandler(
    ILancamentoRepository lancamentoRepository,
    IDateTimeProvider dateTimeProvider,
    TimeZoneInfo fusoDoNegocio) : IQueryHandler<ListarLancamentosDoDiaQuery, IReadOnlyList<LancamentoDoDiaModel>>
{
    /// <inheritdoc />
    public Task<IReadOnlyList<LancamentoDoDiaModel>> HandleAsync(
        ListarLancamentosDoDiaQuery query, CancellationToken cancellationToken = default)
    {
        var dia = query.Data ?? DiaDeNegocio.De(dateTimeProvider.OffsetUtcNow, fusoDoNegocio);
        return lancamentoRepository.ListarDoDiaAsync(query.ComercianteId, dia, cancellationToken);
    }
}
