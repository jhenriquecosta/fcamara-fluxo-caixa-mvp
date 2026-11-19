using Cashflow.Application;
using Cashflow.Application.Decorators.AuditLog;
using Cashflow.Application.Decorators.DatabaseRetry;
using Cashflow.Modules.Consolidado.Entities;
using Cashflow.Modules.Consolidado.Models;
using Cashflow.Modules.Consolidado.Repositories;

namespace Cashflow.Modules.Consolidado.Queries;

public sealed class GetConsolidadoDiarioQuery : IQuery<ConsolidadoDiarioModel>
{
    public required Guid ComercianteId { get; init; }

    public required DateOnly Data { get; init; }
}

/// <summary>Um dia sem movimento responde saldo zero com o acumulado do último dia anterior — nunca 404.</summary>
[AuditLog]
[DatabaseRetry]
public sealed class GetConsolidadoDiarioQueryHandler(IConsolidadoRepository consolidadoRepository)
    : IQueryHandler<GetConsolidadoDiarioQuery, ConsolidadoDiarioModel>
{
    public async Task<ConsolidadoDiarioModel> HandleAsync(GetConsolidadoDiarioQuery query, CancellationToken cancellationToken = default)
    {
        var consolidado = await consolidadoRepository.GetAsync(query.ComercianteId, query.Data, cancellationToken)
                          ?? ConsolidadoDiario.Vazio(query.ComercianteId, query.Data,
                              await consolidadoRepository.SaldoAnteriorAsync(query.ComercianteId, query.Data, cancellationToken));

        return ConsolidadoDiarioModel.De(consolidado);
    }
}
