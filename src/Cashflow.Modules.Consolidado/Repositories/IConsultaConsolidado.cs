using Cashflow.Modules.Consolidado.Entities;

namespace Cashflow.Modules.Consolidado.Repositories;

public interface IConsultaConsolidado
{
    Task<ConsolidadoDiario?> ObterAsync(Guid comercianteId, DateOnly data, CancellationToken ct);
    Task<decimal> SaldoAnteriorAsync(Guid comercianteId, DateOnly data, CancellationToken ct);
}
