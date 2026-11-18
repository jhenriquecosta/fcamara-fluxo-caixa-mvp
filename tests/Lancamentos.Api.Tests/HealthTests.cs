using System.Net;

namespace Cashflow.Lancamentos.Api.Tests;

[Collection("integracao")]
public sealed class HealthTests(PostgresFixture db)
{
    [Fact]
    public async Task Health_responde_200()
    {
        using var factory = new ApiFactory(db.ConnectionString);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
