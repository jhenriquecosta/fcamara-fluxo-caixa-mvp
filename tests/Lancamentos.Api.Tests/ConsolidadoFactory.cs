using Cashflow.Consolidado.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Cashflow.Lancamentos.Api.Tests;

public sealed class ConsolidadoFactory(string connectionString, string kafkaBootstrap)
    : WebApplicationFactory<ConsolidadoApiEntryPoint>
{
    static ConsolidadoFactory()
    {
        Environment.SetEnvironmentVariable("Auth__Issuer", TestTokens.Issuer);
        Environment.SetEnvironmentVariable("Auth__Audience", TestTokens.Audience);
        Environment.SetEnvironmentVariable("Auth__SigningKey", TestTokens.SigningKey);
        Environment.SetEnvironmentVariable("Serilog__MinimumLevel__Default", "Warning");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", connectionString);
        Environment.SetEnvironmentVariable("Kafka__BootstrapServers", kafkaBootstrap);
        builder.UseEnvironment("Testing");
    }
}
