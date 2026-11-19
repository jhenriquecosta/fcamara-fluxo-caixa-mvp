using System.Net;
using Cashflow.Blazor.Modules.Core.Services;
using Cashflow.Blazor.Modules.Lancamentos.Models;
using Cashflow.Blazor.Modules.Lancamentos.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cashflow.BlazorServerSide.IntegrationTests;

/// <summary>O cliente HTTP das telas: bearer, Idempotency-Key e ProblemDetails (PRD-0003 RF6, RF9).</summary>
public sealed class HttpServiceTests
{
    [Fact]
    public async Task RF6_lancar_envia_bearer_e_a_chave_de_idempotencia_recebida()
    {
        var api = new ApiFalsa(_ => ApiFalsa.Json(
            """{"id":"6f1c0a52-0a52-4d6a-9d3a-111111111111","comercianteId":"6f1c0a52-0a52-4d6a-9d3a-222222222222","data":"2026-11-19","valor":10.5,"tipo":"credito","descricao":null,"estornoDe":null}""",
            HttpStatusCode.Created));
        await using var front = new FrontendFactory(() => api);
        await using var sessao = front.Sessao(FrontendFactory.Operador);
        var service = sessao.ServiceProvider.GetRequiredService<LancamentoService>();

        var criado = await service.LancarAsync(new NovoLancamentoModel { Valor = 10.5m, Tipo = "credito", Descricao = "  " },
            "chave-1", TestContext.Current.CancellationToken);

        Assert.Equal(10.5m, criado.Valor);
        var request = Assert.Single(api.Recebidas);
        Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
        Assert.Equal("chave-1", Assert.Single(request.Headers.GetValues("Idempotency-Key")));
        Assert.Contains("\"descricao\":null", Assert.Single(api.Corpos));
    }

    [Fact]
    public async Task RF9_problem_details_vira_mensagem_legivel()
    {
        var api = new ApiFalsa(_ => ApiFalsa.Json(
            """{"title":"lançamento já estornado","status":409}""", HttpStatusCode.Conflict));
        await using var front = new FrontendFactory(() => api);
        await using var sessao = front.Sessao(FrontendFactory.Operador);
        var service = sessao.ServiceProvider.GetRequiredService<LancamentoService>();

        var erro = await Assert.ThrowsAsync<ApiException>(() =>
            service.EstornarAsync(Guid.NewGuid(), "k", TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.Conflict, erro.StatusCode);
        Assert.Equal("lançamento já estornado", erro.Titulo);
    }

    [Fact]
    public async Task RF9_api_fora_do_ar_vira_indisponivel_sem_derrubar_a_tela()
    {
        var api = new ApiFalsa(_ => throw new HttpRequestException("connection refused"));
        await using var front = new FrontendFactory(() => api);
        await using var sessao = front.Sessao(FrontendFactory.Operador);
        var service = sessao.ServiceProvider.GetRequiredService<LancamentoService>();

        var erro = await Assert.ThrowsAsync<ApiException>(() =>
            service.ListarAsync(null, TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, erro.StatusCode);
    }

    [Fact]
    public async Task RF9_sem_sessao_nao_chama_a_api()
    {
        var api = new ApiFalsa(_ => ApiFalsa.Json("[]"));
        await using var front = new FrontendFactory(() => api);
        await using var scope = front.Services.CreateAsyncScope();
        var authProvider = (Microsoft.AspNetCore.Components.Authorization.IHostEnvironmentAuthenticationStateProvider)
            scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider>();
        authProvider.SetAuthenticationState(Task.FromResult(
            new Microsoft.AspNetCore.Components.Authorization.AuthenticationState(new System.Security.Claims.ClaimsPrincipal())));
        var service = scope.ServiceProvider.GetRequiredService<LancamentoService>();

        var erro = await Assert.ThrowsAsync<ApiException>(() =>
            service.ListarAsync(null, TestContext.Current.CancellationToken));

        Assert.True(erro.SessaoExpirada);
        Assert.Empty(api.Recebidas);
    }
}
