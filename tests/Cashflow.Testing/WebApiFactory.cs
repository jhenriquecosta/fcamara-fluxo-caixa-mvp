using Cashflow.WebAPI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cashflow.Testing;

/// <summary>
/// O <c>Cashflow.WebAPI</c> com os módulos que o teste liga — o mesmo binário que o compose sobe
/// como <c>lancamentos-api</c> (só Lançamentos) e como <c>consolidado-api</c> (só Consolidado).
/// </summary>
public sealed class WebApiFactory(
    string connectionString,
    bool lancamentos = true,
    bool consolidado = true,
    DateTimeOffset? agora = null,
    string ambiente = "Testing",
    string? authority = null) : WebApplicationFactory<WebApiEntryPoint>
{
    /// <summary>O WebAPI como <c>lancamentos-api</c>, com o relógio opcionalmente fixo em <paramref name="agora"/>.</summary>
    public static WebApiFactory SoLancamentos(
        string connectionString, DateTimeOffset? agora = null, string ambiente = "Testing", string? authority = null) =>
        new(connectionString, lancamentos: true, consolidado: false, agora, ambiente, authority);

    /// <summary>O WebAPI como <c>consolidado-api</c>.</summary>
    public static WebApiFactory SoConsolidado(
        string connectionString, string ambiente = "Testing", string? authority = null) =>
        new(connectionString, lancamentos: false, consolidado: true, ambiente: ambiente, authority: authority);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        HostSettings.Aplicar(HostSettings.Modulos(connectionString, lancamentos, consolidado));
        // Com Authority, o host valida pelo JWKS do emissor; sem ela, pela chave de desenvolvimento.
        Environment.SetEnvironmentVariable("Auth__Authority", authority);
        builder.UseEnvironment(ambiente);

        if (agora is { } instante)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(new FixedTimeProvider(instante));
            });
        }
    }
}
