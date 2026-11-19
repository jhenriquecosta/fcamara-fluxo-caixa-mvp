using Cashflow.Application;
using Cashflow.Application.Decorators.AuditLog;
using Cashflow.Application.Decorators.DatabaseRetry;
using Cashflow.Modules.Consolidado.Entities;
using Cashflow.Modules.Consolidado.Models;
using Cashflow.Modules.Consolidado.Repositories;

namespace Cashflow.Modules.Consolidado.Queries;

/// <summary>Consultar o saldo de um dia do caixa do comerciante (RF-03).</summary>
public sealed class ConsultarConsolidadoDoDiaQuery : IQuery<ConsolidadoDiarioModel>
{
    /// <summary>O dono do caixa, tirado do token.</summary>
    public required Guid ComercianteId { get; init; }

    /// <summary>O dia de negócio consultado.</summary>
    public required DateOnly Data { get; init; }
}

/// <summary>
/// Responde o consolidado do dia. Um dia sem movimento responde saldo zero com o acumulado do último
/// dia anterior, nunca 404. Repete em falha transitória do banco, já que consulta não muda estado.
/// </summary>
[AuditLog]
[DatabaseRetry]
public sealed class ConsultarConsolidadoDoDiaQueryHandler(IConsolidadoRepository consolidadoRepository)
    : IQueryHandler<ConsultarConsolidadoDoDiaQuery, ConsolidadoDiarioModel>
{
    /// <inheritdoc />
    public async Task<ConsolidadoDiarioModel> HandleAsync(ConsultarConsolidadoDoDiaQuery query, CancellationToken cancellationToken = default)
    {
        var consolidado = await consolidadoRepository.ObterDoDiaAsync(query.ComercianteId, query.Data, cancellationToken)
                          ?? ConsolidadoDiario.SemMovimento(query.ComercianteId, query.Data,
                              await consolidadoRepository.SaldoDeAberturaAsync(query.ComercianteId, query.Data, cancellationToken));

        return ConsolidadoDiarioModel.De(consolidado);
    }
}
