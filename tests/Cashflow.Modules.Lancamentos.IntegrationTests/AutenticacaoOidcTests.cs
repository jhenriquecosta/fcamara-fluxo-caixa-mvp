using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Cashflow.Testing;

namespace Cashflow.Modules.Lancamentos.IntegrationTests;

/// <summary>
/// ADR-0012, item 2: com <c>Auth:Authority</c> o host valida pelo JWKS do emissor — o caminho do
/// Cognito — e o <c>/dev/token</c> deixa de existir mesmo em Development.
/// </summary>
[Collection("integracao")]
public sealed class AutenticacaoOidcTests(PostgresFixture db)
{
    private static readonly Guid Comerciante = Guid.NewGuid();

    private static HttpClient ComToken(HttpClient cliente, string token)
    {
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }

    [Fact]
    [Trait("Criterio", "RF7")]
    public async Task Token_assinado_pela_chave_publicada_no_jwks_e_aceito()
    {
        await using var emissor = await EmissorOidcFalso.IniciarAsync();
        using var api = WebApiFactory.SoLancamentos(db.ConnectionString, authority: emissor.Authority);
        using var cliente = ComToken(api.CreateClient(), emissor.CriarToken(Comerciante, "lancamentos.read"));

        var resposta = await cliente.GetAsync("/api/v1/lancamentos", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    [Trait("Criterio", "RF7")]
    public async Task Token_assinado_por_outra_chave_e_recusado()
    {
        await using var emissor = await EmissorOidcFalso.IniciarAsync();
        using var api = WebApiFactory.SoLancamentos(db.ConnectionString, authority: emissor.Authority);
        using var cliente = ComToken(api.CreateClient(), emissor.CriarTokenDeOutraChave(Comerciante, "lancamentos.read"));

        var resposta = await cliente.GetAsync("/api/v1/lancamentos", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    [Trait("Criterio", "RF7")]
    public async Task Com_oidc_o_dev_token_nao_e_mapeado_nem_em_development()
    {
        await using var emissor = await EmissorOidcFalso.IniciarAsync();
        using var api = WebApiFactory.SoLancamentos(
            db.ConnectionString, ambiente: "Development", authority: emissor.Authority);
        using var cliente = api.CreateClient();

        var resposta = await cliente.PostAsJsonAsync("/dev/token",
            new { comercianteId = Comerciante, scopes = new[] { "lancamentos.read" } },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }
}
