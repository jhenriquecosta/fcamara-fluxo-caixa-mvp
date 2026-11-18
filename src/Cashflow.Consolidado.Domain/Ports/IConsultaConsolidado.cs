namespace Cashflow.Consolidado.Domain.Ports;

public interface IConsultaConsolidado
{
    Task<ConsolidadoDiario?> ObterAsync(Guid comercianteId, DateOnly data, CancellationToken ct);
    Task<decimal> SaldoAnteriorAsync(Guid comercianteId, DateOnly data, CancellationToken ct);
}
