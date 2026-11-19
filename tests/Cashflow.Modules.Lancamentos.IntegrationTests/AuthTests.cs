using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Cashflow.Testing;

namespace Cashflow.Modules.Lancamentos.IntegrationTests;

/// <summary>Só escreve no caixa quem traz um token válido com o escopo <c>lancamentos.write</c>.</summary>
[Collection("integracao")]
public sealed class AuthTests(PostgresFixture db)
{
    private static readonly object LancamentoValido = new { valor = 100.00m, tipo = "debito" };

    [Fact]
    [Trait("Criterio", "AC11")]
    public async Task Requisicao_sem_token_responde_401()
    {
        using var api = WebApiFactory.SoLancamentos(db.ConnectionString);
        using var anonimo = api.CreateClient();

        var resposta = await anonimo.PostAsJsonAsync("/api/v1/lancamentos", LancamentoValido, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    [Trait("Criterio", "AC11")]
    public async Task Token_valido_sem_o_escopo_de_escrita_responde_403()
    {
        using var api = WebApiFactory.SoLancamentos(db.ConnectionString);
        using var soLeitura = api.CreateClient();
        soLeitura.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestTokens.Create(Guid.NewGuid(), "consolidado.read"));

        var resposta = await soLeitura.PostAsJsonAsync("/api/v1/lancamentos", LancamentoValido, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }
}
