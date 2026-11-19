using Cashflow.Blazor.Modules.Core.Authorization;
using Spectre.Console.Testing;

namespace Cashflow.Terminal.Tests;

/// <summary>PRD-0004 RF5/AC4: lançar pela API, com a chave de idempotência que o contrato exige.</summary>
public sealed class TelaLancarTests
{
    private static readonly Guid Id = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static ApiFalsa ApiComLancamentoCriado() => new(_ => ApiFalsa.Json(
        $$"""{"id":"{{Id}}","comercianteId":"22222222-2222-2222-2222-222222222222","data":"2026-11-19","valor":1500,"tipo":"credito","descricao":"Venda do dia","estornoDe":null}""",
        201));

    [Fact]
    [Trait("Criterio", "AC4")]
    public async Task Operador_lanca_e_o_terminal_mostra_o_id_criado()
    {
        var api = ApiComLancamentoCriado();
        var console = new TestConsole();
        console.Input.PushTextWithEnter("credito");
        console.Input.PushTextWithEnter("1500");
        console.Input.PushTextWithEnter("Venda do dia");

        await new TelaLancar(console, SessaoDeTeste.Para(Perfis.Operador, api))
            .ExecutarAsync(TestContext.Current.CancellationToken);

        var post = Assert.Single(api.Recebidas, r => r.Metodo == HttpMethod.Post);
        Assert.Equal("http://api/api/v1/lancamentos", post.Url.ToString());
        Assert.False(string.IsNullOrWhiteSpace(post.ChaveDeIdempotencia));
        Assert.Contains("\"tipo\":\"credito\"", post.Corpo);
        Assert.Contains("\"valor\":1500", post.Corpo);
        Assert.Contains(Id.ToString(), console.Output);
    }

    [Fact]
    [Trait("Criterio", "AC4")]
    public async Task Perfil_de_consulta_nao_lanca_nem_chama_a_api()
    {
        var api = ApiComLancamentoCriado();
        var console = new TestConsole();

        await new TelaLancar(console, SessaoDeTeste.Para(Perfis.Consulta, api))
            .ExecutarAsync(TestContext.Current.CancellationToken);

        Assert.Empty(api.Recebidas);
        Assert.Contains("sem permissão", console.Output);
    }
}
