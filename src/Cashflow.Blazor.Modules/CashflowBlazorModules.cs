using Cashflow.Blazor.Modules.Consolidado.Services;
using Cashflow.Blazor.Modules.Core.Services;
using Cashflow.Blazor.Modules.Lancamentos.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>O registro dos clientes HTTP das telas, um por serviço da API.</summary>
public static class CashflowBlazorModules
{
    /// <summary>
    /// Os clientes de cada módulo, um <c>HttpClient</c> por serviço da API: o consolidado fora do ar
    /// não afeta o cliente de lançamentos. O host registra o <c>ITokenManager</c>.
    /// </summary>
    public static IServiceCollection AddCashflowBlazorModules(
        this IServiceCollection services, Uri lancamentosEndpoint, Uri consolidadoEndpoint)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<DiaDeNegocio>();

        services.AddHttpClient<LancamentoService>(client =>
        {
            client.BaseAddress = lancamentosEndpoint;
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddHttpClient<ConsolidadoService>(client =>
        {
            client.BaseAddress = consolidadoEndpoint;
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        return services;
    }
}
