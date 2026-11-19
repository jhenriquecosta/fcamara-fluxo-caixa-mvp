using Cashflow.Blazor.Modules.Core.Authorization;
using Spectre.Console.Testing;

namespace Cashflow.Terminal.Tests;

/// <summary>PRD-0004 RF5/AC4: o dia listado com a marca de estornado e o estorno só para o operador.</summary>
public sealed class TelaLancamentosTests
{
    private static readonly Guid Normal = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111");
    private static readonly Guid Estornado = Guid.Parse("bbbbbbbb-2222-2222-2222-222222222222");
    private static readonly Guid Estorno = Guid.Parse("cccccccc-3333-3333-3333-333333333333");

    private static string Corpo(IReadOnlyList<ApiFalsa.Requisicao> _) =>
        """
        [{"id":"aaaaaaaa-1111-1111-1111-111111111111","data":"2026-11-19","valor":1500,"tipo":"credito","descricao":"Venda do dia","estornoDe":null,"estornado":false,"criadoEm":"2026-11-19T12:00:00+00:00"},
         {"id":"bbbbbbbb-2222-2222-2222-222222222222","data":"2026-11-19","valor":100,"tipo":"debito","descricao":"Café","estornoDe":null,"estornado":true,"criadoEm":"2026-11-19T13:00:00+00:00"}]
        """;

    private static ApiFalsa Api() => new(request => request.Method == HttpMethod.Post
        ? ApiFalsa.Json(
            """{"id":"cccccccc-3333-3333-3333-333333333333","comercianteId":"22222222-2222-2222-2222-222222222222","data":"2026-11-19","valor":1500,"tipo":"debito","descricao":null,"estornoDe":"aaaaaaaa-1111-1111-1111-111111111111"}""",
            201)
        : ApiFalsa.Json(Corpo([])));

    [Fact]
    [Trait("Criterio", "AC4")]
    public async Task Operador_ve_o_dia_com_a_marca_e_estorna_o_lancamento_escolhido()
    {
        var api = Api();
        var console = new TestConsole();
        console.Input.PushTextWithEnter("1");

        await new TelaLancamentos(console, SessaoDeTeste.Para(Perfis.Operador, api))
            .ExecutarAsync(TestContext.Current.CancellationToken);

        Assert.Contains("Venda do dia", console.Output);
        Assert.Contains("estornado", console.Output, StringComparison.OrdinalIgnoreCase);
        var post = Assert.Single(api.Recebidas, r => r.Metodo == HttpMethod.Post);
        Assert.Equal($"http://api/api/v1/lancamentos/{Normal}/estornos", post.Url.ToString());
        Assert.False(string.IsNullOrWhiteSpace(post.ChaveDeIdempotencia));
        Assert.Contains(Estorno.ToString(), console.Output);
    }

    [Fact]
    [Trait("Criterio", "AC4")]
    public async Task Perfil_de_consulta_ve_o_dia_sem_poder_estornar()
    {
        var api = Api();
        var console = new TestConsole();

        await new TelaLancamentos(console, SessaoDeTeste.Para(Perfis.Consulta, api))
            .ExecutarAsync(TestContext.Current.CancellationToken);

        Assert.Contains("Venda do dia", console.Output);
        Assert.Contains("somente leitura", console.Output);
        Assert.DoesNotContain("Estornar", console.Output);
        Assert.DoesNotContain(api.Recebidas, r => r.Metodo == HttpMethod.Post);
    }
}
