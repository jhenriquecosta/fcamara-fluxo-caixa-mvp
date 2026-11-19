using System.Net;
using Cashflow.Testing;

namespace Cashflow.Modules.Lancamentos.IntegrationTests;

/// <summary>O orquestrador sabe se a API está viva — e se está pronta — sem precisar de token (RF-8).</summary>
[Collection("integracao")]
public sealed class HealthTests(PostgresFixture db)
{
    [Fact]
    public async Task Health_responde_200_sem_autenticacao()
    {
        using var api = WebApiFactory.SoLancamentos(db.ConnectionString);
        using var anonimo = api.CreateClient();

        var resposta = await anonimo.GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    [Trait("Criterio", "RF8")]
    public async Task Live_responde_200_sem_autenticacao()
    {
        using var api = WebApiFactory.SoLancamentos(db.ConnectionString);
        using var anonimo = api.CreateClient();

        var resposta = await anonimo.GetAsync("/health/live", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    [Trait("Criterio", "RF8")]
    public async Task Ready_responde_200_quando_o_banco_responde()
    {
        using var api = WebApiFactory.SoLancamentos(db.ConnectionString);
        using var anonimo = api.CreateClient();

        var resposta = await anonimo.GetAsync("/health/ready", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    [Trait("Criterio", "RF8")]
    public async Task Ready_responde_503_quando_o_banco_nao_responde()
    {
        var semBanco = "Host=127.0.0.1;Port=1;Database=x;Username=x;Password=x;Timeout=1;Command Timeout=1";
        using var api = WebApiFactory.SoLancamentos(semBanco);
        using var anonimo = api.CreateClient();

        var resposta = await anonimo.GetAsync("/health/ready", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, resposta.StatusCode);
    }
}
