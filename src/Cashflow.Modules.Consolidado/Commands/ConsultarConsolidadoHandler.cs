using Cashflow.Modules.Consolidado.Entities;
using Cashflow.Modules.Consolidado.Repositories;

namespace Cashflow.Modules.Consolidado.Commands;

public sealed record ConsolidadoResponse(
    Guid ComercianteId, DateOnly Data, decimal SaldoDia, decimal SaldoAnterior, decimal SaldoAcumulado);

public sealed class ConsultarConsolidadoHandler(IConsultaConsolidado consulta)
{
    public async Task<ConsolidadoResponse> HandleAsync(
        Guid comercianteId, DateOnly data, CancellationToken ct)
    {
        var linha = await consulta.ObterAsync(comercianteId, data, ct);

        if (linha is null)
        {
            var anterior = await consulta.SaldoAnteriorAsync(comercianteId, data, ct);
            linha = ConsolidadoDiario.Vazio(comercianteId, data, anterior);
        }

        return new ConsolidadoResponse(
            linha.ComercianteId, linha.Data, linha.SaldoDia,
            linha.SaldoAnterior, linha.SaldoAcumulado);
    }
}
