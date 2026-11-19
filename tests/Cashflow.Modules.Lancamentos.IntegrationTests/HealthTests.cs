using Cashflow.Testing;
using System.Net;

namespace Cashflow.Modules.Lancamentos.IntegrationTests;

[Collection("integracao")]
public sealed class HealthTests(PostgresFixture db)
{
    [Fact]
    public async Task Health_responde_200()
    {
        using var factory = WebApiFactory.SoLancamentos(db.ConnectionString);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
