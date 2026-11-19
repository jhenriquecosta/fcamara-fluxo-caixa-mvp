using Cashflow.Application;
using Cashflow.Application.Decorators.AuditLog;
using Cashflow.Application.Decorators.DatabaseRetry;
using Cashflow.CrossCuttingConcerns.DateTimes;
using Cashflow.Modules.Lancamentos.Entities;
using Cashflow.Modules.Lancamentos.Models;
using Cashflow.Modules.Lancamentos.Repositories;

namespace Cashflow.Modules.Lancamentos.Queries;

public sealed class ListarLancamentosDoDiaQuery : IQuery<IReadOnlyList<LancamentoDoDiaModel>>
{
    public required Guid ComercianteId { get; init; }

    /// <summary>O dia de negócio; nulo quer dizer hoje em America/Sao_Paulo.</summary>
    public DateOnly? Data { get; init; }
}

[AuditLog]
[DatabaseRetry]
public sealed class ListarLancamentosDoDiaQueryHandler(
    ILancamentoRepository lancamentoRepository,
    IDateTimeProvider dateTimeProvider,
    TimeZoneInfo fusoDoNegocio) : IQueryHandler<ListarLancamentosDoDiaQuery, IReadOnlyList<LancamentoDoDiaModel>>
{
    public Task<IReadOnlyList<LancamentoDoDiaModel>> HandleAsync(
        ListarLancamentosDoDiaQuery query, CancellationToken cancellationToken = default)
    {
        var dia = query.Data ?? DiaDeNegocio.De(dateTimeProvider.OffsetUtcNow, fusoDoNegocio);
        return lancamentoRepository.ListarDoDiaAsync(query.ComercianteId, dia, cancellationToken);
    }
}
