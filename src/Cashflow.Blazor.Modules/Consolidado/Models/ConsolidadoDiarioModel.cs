namespace Cashflow.Blazor.Modules.Consolidado.Models;

/// <summary>A resposta de <c>GET /api/v1/consolidado/{data}</c>.</summary>
public sealed record ConsolidadoDiarioModel(
    Guid ComercianteId, DateOnly Data, decimal SaldoDia, decimal SaldoAnterior, decimal SaldoAcumulado);
