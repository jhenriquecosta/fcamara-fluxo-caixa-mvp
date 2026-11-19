namespace Cashflow.Modules.Consolidado.Entities;

/// <summary>
/// A projeção do dia por comerciante (ADR-0005). É um modelo de leitura alimentado por eventos,
/// não um agregado: não nasce de comando nem tem invariante própria além do saldo acumulado.
/// </summary>
public sealed record ConsolidadoDiario(Guid ComercianteId, DateOnly Data, decimal SaldoDia, decimal SaldoAcumulado)
{
    public decimal SaldoAnterior => SaldoAcumulado - SaldoDia;

    public static ConsolidadoDiario Vazio(Guid comercianteId, DateOnly data, decimal saldoAnterior) =>
        new(comercianteId, data, 0m, saldoAnterior);
}
