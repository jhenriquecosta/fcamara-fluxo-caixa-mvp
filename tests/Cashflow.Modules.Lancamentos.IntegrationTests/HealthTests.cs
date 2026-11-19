using System.Net;
using Cashflow.Testing;

namespace Cashflow.Modules.Lancamentos.IntegrationTests;

/// <summary>O orquestrador sabe se a API está viva sem precisar de token.</summary>
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
}
