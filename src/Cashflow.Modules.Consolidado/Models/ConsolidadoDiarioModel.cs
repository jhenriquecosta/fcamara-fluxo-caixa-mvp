using Cashflow.Modules.Consolidado.Entities;

namespace Cashflow.Modules.Consolidado.Models;

public sealed record ConsolidadoDiarioModel(
    Guid ComercianteId, DateOnly Data, decimal SaldoDia, decimal SaldoAnterior, decimal SaldoAcumulado)
{
    public static ConsolidadoDiarioModel De(ConsolidadoDiario consolidado) =>
        new(consolidado.ComercianteId, consolidado.Data, consolidado.SaldoDia,
            consolidado.SaldoAnterior, consolidado.SaldoAcumulado);
}
