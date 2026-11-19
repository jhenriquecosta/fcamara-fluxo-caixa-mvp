using Cashflow.Modules.Consolidado.Entities;

namespace Cashflow.Modules.Consolidado.Models;

/// <summary>O consolidado do dia como a API o devolve, com o saldo de abertura explícito.</summary>
public sealed record ConsolidadoDiarioModel(
    Guid ComercianteId, DateOnly Data, decimal SaldoDia, decimal SaldoAnterior, decimal SaldoAcumulado)
{
    /// <summary>Projeta a leitura no modelo da API.</summary>
    public static ConsolidadoDiarioModel De(ConsolidadoDiario consolidado) =>
        new(consolidado.ComercianteId, consolidado.Data, consolidado.SaldoDia,
            consolidado.SaldoAnterior, consolidado.SaldoAcumulado);
}
