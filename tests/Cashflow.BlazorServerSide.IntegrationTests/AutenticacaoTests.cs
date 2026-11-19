using System.Net;

namespace Cashflow.BlazorServerSide.IntegrationTests;

/// <summary>PRD-0003 RF1–RF3 e critérios 1, 2 e 6, pelo HTTP que o navegador usaria.</summary>
public sealed class AutenticacaoTests
{
    private static FrontendFactory Front(ApiFalsa? api = null)
    {
        var handler = api ?? ApiFalsa.Vazia();
        return new FrontendFactory(() => handler);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/lancamentos")]
    [InlineData("/lancamentos/novo")]
    [InlineData("/consolidado")]
    public async Task AC1_rota_protegida_sem_sessao_redireciona_para_login(string rota)
    {
        await using var front = Front();
        using var navegador = front.Navegador();

        var resposta = await navegador.GetAsync(rota, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.StartsWith("/login", resposta.Headers.Location!.AbsolutePath);
        Assert.Contains($"ReturnUrl={Uri.EscapeDataString(rota)}", resposta.Headers.Location.Query);
    }

    [Theory]
    [InlineData("operador", "errada")]
    [InlineData("ninguem", FrontendFactory.Senha)]
    [InlineData("", "")]
    public async Task AC1_credencial_invalida_nao_autentica(string usuario, string senha)
    {
        await using var front = Front();
        using var navegador = front.Navegador();

        var resposta = await FrontendFactory.EntrarAsync(navegador, usuario, senha, "/lancamentos");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.False(resposta.Headers.TryGetValues("Set-Cookie", out var cookies)
                     && cookies.Any(c => c.StartsWith("cashflow.session=", StringComparison.Ordinal)));
        var depois = await navegador.GetAsync("/lancamentos", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Redirect, depois.StatusCode);
    }

    [Fact]
    public async Task AC1_mesma_mensagem_para_usuario_inexistente_e_senha_errada()
    {
        await using var front = Front();
        using var navegador = front.Navegador();

        var senhaErrada = await (await FrontendFactory.EntrarAsync(navegador, "operador", "errada"))
            .Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var inexistente = await (await FrontendFactory.EntrarAsync(navegador, "ninguem", "errada"))
            .Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        const string mensagem = "Usu&#xE1;rio ou senha inv&#xE1;lidos.";
        Assert.Contains(mensagem, senhaErrada);
        Assert.Contains(mensagem, inexistente);
    }

    [Fact]
    public async Task RF1_login_valido_cria_sessao_e_volta_para_o_returnUrl()
    {
        await using var front = Front();
        using var navegador = front.Navegador();

        var resposta = await FrontendFactory.EntrarAsync(navegador, "operador", FrontendFactory.Senha, "/consolidado");

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.Equal("/consolidado", resposta.Headers.Location!.AbsolutePath);
        var cookie = Assert.Single(resposta.Headers.GetValues("Set-Cookie"), c => c.StartsWith("cashflow.session="));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);

        var depois = await navegador.GetAsync("/consolidado", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, depois.StatusCode);
    }

    [Theory]
    [InlineData("https://evil.example/roubo")]
    [InlineData("//evil.example")]
    public async Task RF1_returnUrl_externo_volta_para_o_inicio(string returnUrl)
    {
        await using var front = Front();
        using var navegador = front.Navegador();

        var resposta = await FrontendFactory.EntrarAsync(navegador, "operador", FrontendFactory.Senha, returnUrl);

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.Equal("/", resposta.Headers.Location!.OriginalString.Replace("http://localhost", ""));
    }

    [Fact]
    public async Task AC2_perfil_consulta_nao_acessa_lancar()
    {
        await using var front = Front();
        using var navegador = front.Navegador();
        await FrontendFactory.EntrarAsync(navegador, "consulta", FrontendFactory.Senha);

        var novo = await navegador.GetAsync("/lancamentos/novo", TestContext.Current.CancellationToken);
        var lista = await navegador.GetStringAsync("/lancamentos", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, novo.StatusCode);
        Assert.Equal("/acesso-negado", novo.Headers.Location!.AbsolutePath);
        Assert.Contains("Lançamentos", lista);
        Assert.DoesNotContain("Novo lançamento", lista);
        Assert.DoesNotContain(">Estornar<", lista);
    }

    [Fact]
    public async Task AC2_perfil_operador_acessa_lancar()
    {
        await using var front = Front();
        using var navegador = front.Navegador();
        await FrontendFactory.EntrarAsync(navegador, "operador", FrontendFactory.Senha);

        var novo = await navegador.GetAsync("/lancamentos/novo", TestContext.Current.CancellationToken);
        var html = await novo.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, novo.StatusCode);
        Assert.Contains("Registrar lançamento", html);
    }

    [Fact]
    public async Task AC6_o_token_de_acesso_nunca_chega_ao_navegador()
    {
        var api = ApiFalsa.Vazia();
        await using var front = Front(api);
        using var navegador = front.Navegador();
        var login = await FrontendFactory.EntrarAsync(navegador, "operador", FrontendFactory.Senha);

        var html = await navegador.GetStringAsync("/lancamentos", TestContext.Current.CancellationToken);

        var token = Assert.Single(api.Bearers.Distinct());
        Assert.StartsWith("eyJ", token);
        Assert.DoesNotContain(token, html);
        Assert.DoesNotContain("eyJ", html);
        Assert.All(login.Headers.GetValues("Set-Cookie"), c => Assert.DoesNotContain("eyJ", c));
    }

    [Fact]
    public async Task RF3_logout_exige_antiforgery_e_encerra_a_sessao()
    {
        await using var front = Front();
        using var navegador = front.Navegador();
        await FrontendFactory.EntrarAsync(navegador, "operador", FrontendFactory.Senha);

        var semToken = await navegador.PostAsync("/logout", new FormUrlEncodedContent([]),
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, semToken.StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await navegador.GetAsync("/lancamentos", TestContext.Current.CancellationToken)).StatusCode);

        var pagina = await navegador.GetStringAsync("/lancamentos", TestContext.Current.CancellationToken);
        var antiforgery = System.Text.RegularExpressions.Regex
            .Match(pagina, "name=\"__RequestVerificationToken\" value=\"([^\"]+)\"").Groups[1].Value;
        var sair = await navegador.PostAsync("/logout", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = antiforgery
        }), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, sair.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect,
            (await navegador.GetAsync("/lancamentos", TestContext.Current.CancellationToken)).StatusCode);
    }
}
