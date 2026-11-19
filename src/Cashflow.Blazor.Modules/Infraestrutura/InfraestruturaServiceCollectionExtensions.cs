using Cashflow.Blazor.Modules.Infraestrutura;
using Cashflow.Blazor.Modules.Infraestrutura.Services;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>O registro dos clientes de infraestrutura que o painel web e o terminal usam (ADR-0011).</summary>
public static class InfraestruturaServiceCollectionExtensions
{
    /// <summary>Registra saúde, Prometheus e logs a partir da seção <c>Infraestrutura</c>.</summary>
    public static IServiceCollection AddCashflowInfraestrutura(this IServiceCollection services, IConfiguration configuration)
    {
        var secao = configuration.GetSection(InfraestruturaOptions.Secao);
        services.Configure<InfraestruturaOptions>(secao);
        var opcoes = secao.Get<InfraestruturaOptions>() ?? new InfraestruturaOptions();

        services.AddHttpClient<SaudeService>();
        services.AddHttpClient<PrometheusService>(c =>
        {
            c.BaseAddress = ComBarra(opcoes.PrometheusUrl);
            c.Timeout = TimeSpan.FromSeconds(5);
        });
        services.AddHttpClient<LogsService>(c =>
        {
            c.BaseAddress = ComBarra(opcoes.ElasticsearchUrl);
            c.Timeout = TimeSpan.FromSeconds(5);
        });
        return services;
    }

    private static Uri ComBarra(string url) => new(url.EndsWith('/') ? url : url + "/");
}
