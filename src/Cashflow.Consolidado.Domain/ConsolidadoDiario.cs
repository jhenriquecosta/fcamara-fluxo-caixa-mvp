namespace Cashflow.Consolidado.Domain;

public sealed record ConsolidadoDiario(
    Guid ComercianteId, DateOnly Data, decimal SaldoDia, decimal SaldoAcumulado)
{
    public decimal SaldoAnterior => SaldoAcumulado - SaldoDia;

    public static ConsolidadoDiario Vazio(Guid comercianteId, DateOnly data, decimal saldoAnterior) =>
        new(comercianteId, data, 0m, saldoAnterior);
}
