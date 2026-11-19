using System.Globalization;
using Cashflow.Blazor.Modules.Consolidado.Models;
using Cashflow.Blazor.Modules.Core.Services;

namespace Cashflow.Blazor.Modules.Consolidado.Services;

/// <summary>O cliente do serviço de Consolidado, com endereço próprio: se ele cair, Lançamentos segue (ADR-0001).</summary>
public class ConsolidadoService(HttpClient httpClient, ITokenManager tokenManager) : HttpService(httpClient, tokenManager)
{
    public Task<ConsolidadoDiarioModel> ObterAsync(DateOnly data, CancellationToken cancellationToken = default) =>
        GetAsync<ConsolidadoDiarioModel>(
            $"api/v1/consolidado/{data.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}", cancellationToken);
}
