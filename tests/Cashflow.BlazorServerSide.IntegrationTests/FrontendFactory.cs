using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Cashflow.Blazor.Modules.Consolidado.Services;
using Cashflow.Blazor.Modules.Lancamentos.Services;
using Cashflow.BlazorServerSide.ConfigurationOptions;
using Cashflow.BlazorServerSide.Services;
using Cashflow.Testing;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Cashflow.BlazorServerSide.IntegrationTests;

/// <summary>
/// O frontend com usuários de teste e os clientes HTTP apontados para <paramref name="api"/>: a API
/// real em memória (WebApiFactory) ou um handler falso. A configuração entra por variável de
/// ambiente porque o Program a lê antes do Build, como nos demais hosts.
/// </summary>
public sealed class FrontendFactory(Func<HttpMessageHandler> api) : WebApplicationFactory<BlazorServerSideEntryPoint>
{
    public const string Senha = "Senha@123";
    public static readonly Guid Comerciante = Guid.NewGuid();

    private static readonly string Hash = new PasswordHasher<UsuarioOptions>().HashPassword(new UsuarioOptions(), Senha);

    public static readonly UsuarioOptions Operador = Usuario("operador", "Ana Operadora", "operador");
    public static readonly UsuarioOptions Consulta = Usuario("consulta", "Caio Consulta", "consulta");

    private static UsuarioOptions Usuario(string login, string nome, string perfil) => new()
    {
        Usuario = login, Nome = nome, SenhaHash = Hash, ComercianteId = Comerciante, Perfil = perfil
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("Auth__Issuer", TestTokens.Issuer);
        Environment.SetEnvironmentVariable("Auth__Audience", TestTokens.Audience);
        Environment.SetEnvironmentVariable("Auth__SigningKey", TestTokens.SigningKey);
        Environment.SetEnvironmentVariable("Serilog__MinimumLevel__Default", "Warning");
        foreach (var (u, i) in new[] { Operador, Consulta }.Select((u, i) => (u, i)))
        {
            Environment.SetEnvironmentVariable($"Auth__Users__{i}__Usuario", u.Usuario);
            Environment.SetEnvironmentVariable($"Auth__Users__{i}__Nome", u.Nome);
            Environment.SetEnvironmentVariable($"Auth__Users__{i}__SenhaHash", u.SenhaHash);
            Environment.SetEnvironmentVariable($"Auth__Users__{i}__ComercianteId", u.ComercianteId.ToString());
            Environment.SetEnvironmentVariable($"Auth__Users__{i}__Perfil", u.Perfil);
        }

        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.AddHttpClient<LancamentoService>().ConfigurePrimaryHttpMessageHandler(api);
            services.AddHttpClient<ConsolidadoService>().ConfigurePrimaryHttpMessageHandler(api);
        });
    }

    public HttpClient Navegador() =>
        CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    /// <summary>O formulário de login de verdade: GET para o antiforgery, POST com usuário e senha.</summary>
    public static async Task<HttpResponseMessage> EntrarAsync(HttpClient navegador, string usuario, string senha,
        string returnUrl = "/")
    {
        var ct = TestContext.Current.CancellationToken;
        var url = $"/login?ReturnUrl={Uri.EscapeDataString(returnUrl)}";
        var pagina = await navegador.GetStringAsync(url, ct);
        var antiforgery = Regex.Match(pagina, "name=\"__RequestVerificationToken\" value=\"([^\"]+)\"").Groups[1].Value;

        return await navegador.PostAsync(url, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = antiforgery,
            ["_handler"] = "login",
            ["Input.Usuario"] = usuario,
            ["Input.Senha"] = senha
        }), ct);
    }

    /// <summary>Um escopo com a sessão do usuário, como o circuito Blazor teria: os serviços das telas.</summary>
    public AsyncServiceScope Sessao(UsuarioOptions usuario)
    {
        var scope = Services.CreateAsyncScope();
        var provider = (IHostEnvironmentAuthenticationStateProvider)
            scope.ServiceProvider.GetRequiredService<AuthenticationStateProvider>();
        provider.SetAuthenticationState(Task.FromResult(new AuthenticationState(UsuarioStore.Principal(usuario))));
        return scope;
    }
}

/// <summary>A API falsa: responde fixo e guarda o que recebeu, para provar o que a tela enviou.</summary>
public sealed class ApiFalsa(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
{
    public List<HttpRequestMessage> Recebidas { get; } = new();

    public List<string> Bearers { get; } = new();

    /// <summary>O corpo lido no envio: o cliente descarta a mensagem logo depois.</summary>
    public List<string?> Corpos { get; } = new();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var corpo = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        lock (Recebidas)
        {
            Recebidas.Add(request);
            Corpos.Add(corpo);
            if (request.Headers.Authorization?.Parameter is { } token)
                Bearers.Add(token);
        }
        return responder(request);
    }

    /// <summary>Lista vazia para lançamentos, saldo zero para o consolidado: o mínimo para as telas renderizarem.</summary>
    public static ApiFalsa Vazia() => new(request => request.RequestUri!.AbsolutePath.Contains("/consolidado/")
        ? Json($$"""{"comercianteId":"{{Guid.Empty}}","data":"2026-11-19","saldoDia":0,"saldoAnterior":0,"saldoAcumulado":0}""")
        : Json("[]"));

    public static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };
}
