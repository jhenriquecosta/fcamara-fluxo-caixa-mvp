using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cashflow.Lancamentos.Api.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly DateTimeOffset? _now;

    static ApiFactory()
    {
        // O appsettings define a chave de desenvolvimento; o teste sobrescreve
        // por variável de ambiente, que tem precedência sobre o JSON.
        Environment.SetEnvironmentVariable("Auth__Issuer", TestTokens.Issuer);
        Environment.SetEnvironmentVariable("Auth__Audience", TestTokens.Audience);
        Environment.SetEnvironmentVariable("Auth__SigningKey", TestTokens.SigningKey);
        Environment.SetEnvironmentVariable("Serilog__MinimumLevel__Default", "Warning");
    }

    public ApiFactory(string connectionString, DateTimeOffset? now = null)
    {
        _connectionString = connectionString;
        _now = now;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", _connectionString);
        builder.UseEnvironment("Testing");

        if (_now is not null)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(new FixedTimeProvider(_now.Value));
            });
        }
    }
}

public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
