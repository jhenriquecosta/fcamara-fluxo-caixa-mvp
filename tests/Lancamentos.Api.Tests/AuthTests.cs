using System.Net;
using System.Net.Http.Json;

namespace Cashflow.Lancamentos.Api.Tests;

[Collection("integracao")]
public sealed class AuthTests(PostgresFixture db)
{
    [Fact]
    public async Task AC11_sem_token_responde_401()
    {
        using var factory = new ApiFactory(db.ConnectionString);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/lancamentos",
            new { valor = 100.00m, tipo = "debito" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AC11_token_sem_escopo_responde_403()
    {
        using var factory = new ApiFactory(db.ConnectionString);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", TestTokens.Create(Guid.NewGuid(), "consolidado.read"));

        var response = await client.PostAsJsonAsync("/api/v1/lancamentos",
            new { valor = 100.00m, tipo = "debito" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
