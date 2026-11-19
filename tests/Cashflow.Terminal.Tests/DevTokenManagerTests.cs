using System.Text;

namespace Cashflow.Terminal.Tests;

/// <summary>PRD-0004 RF4: o terminal obtém o token do <c>/dev/token</c> e o reaproveita.</summary>
public sealed class DevTokenManagerTests
{
    private static readonly Guid Comerciante = Guid.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae7");

    private static string Jwt(long expiraEmSegundos)
    {
        static string Base64Url(string texto) => Convert.ToBase64String(Encoding.UTF8.GetBytes(texto))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return Base64Url("""{"alg":"HS256"}""") + "." + Base64Url($$"""{"exp":{{expiraEmSegundos}}}""") + ".assinatura";
    }

    [Fact]
    [Trait("Criterio", "AC4")]
    public async Task Pede_o_token_ao_dev_token_e_o_reaproveita_ate_o_vencimento()
    {
        var jwt = Jwt(DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds());
        var api = new ApiFalsa(_ => ApiFalsa.Json($$"""{"token":"{{jwt}}"}"""));
        var gerenciador = new DevTokenManager(
            new HttpClient(api) { BaseAddress = new Uri("http://api/") }, Comerciante, EscoposDoContrato.DoPerfil("operador"));

        var primeiro = await gerenciador.GetTokenAsync(TestContext.Current.CancellationToken);
        var segundo = await gerenciador.GetTokenAsync(TestContext.Current.CancellationToken);

        Assert.Equal(jwt, primeiro.AccessToken);
        Assert.Equal(jwt, segundo.AccessToken);
        Assert.Same(primeiro, segundo);
        var requisicao = Assert.Single(api.Recebidas);
        Assert.Equal("http://api/dev/token", requisicao.Url.ToString());
        Assert.Contains(Comerciante.ToString(), requisicao.Corpo);
        Assert.Contains("lancamentos.write", requisicao.Corpo);
    }
}
