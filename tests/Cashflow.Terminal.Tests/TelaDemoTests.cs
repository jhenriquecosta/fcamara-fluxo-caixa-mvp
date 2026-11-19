using Cashflow.Blazor.Modules.Core.Authorization;
using Spectre.Console.Testing;

namespace Cashflow.Terminal.Tests;

/// <summary>PRD-0004 RF3/AC2: a demo narra o fluxo inteiro e prova a idempotência com a mesma chave.</summary>
public sealed class TelaDemoTests
{
    private const string CreditoId = "aaaaaaaa-1111-1111-1111-111111111111";
    private const string DebitoId = "bbbbbbbb-2222-2222-2222-222222222222";
    private const string EstornoId = "cccccccc-3333-3333-3333-333333333333";

    private static string Criado(string id, string tipo, decimal valor) =>
        $$"""{"id":"{{id}}","comercianteId":"22222222-2222-2222-2222-222222222222","data":"2026-11-19","valor":{{valor.ToString(System.Globalization.CultureInfo.InvariantCulture)}},"tipo":"{{tipo}}","descricao":null,"estornoDe":null}""";

    private static ApiFalsa Api() => new(request =>
    {
        if (request.Method == HttpMethod.Get)
            return ApiFalsa.Json(
                """{"comercianteId":"22222222-2222-2222-2222-222222222222","data":"2026-11-19","saldoDia":1179.50,"saldoAnterior":0,"saldoAcumulado":1179.50}""");
        return request.RequestUri!.AbsolutePath.EndsWith("/estornos")
            ? ApiFalsa.Json(
                $$"""{"id":"{{EstornoId}}","comercianteId":"22222222-2222-2222-2222-222222222222","data":"2026-11-19","valor":1500,"tipo":"debito","descricao":null,"estornoDe":"{{CreditoId}}"}""",
                201)
            : ApiFalsa.Json(
                request.Content!.ReadAsStringAsync().Result.Contains("credito")
                    ? Criado(CreditoId, "credito", 1500)
                    : Criado(DebitoId, "debito", 320.50m),
                201);
    });

    [Fact]
    [Trait("Criterio", "AC2")]
    public async Task Demo_narra_o_fluxo_e_prova_a_idempotencia_com_a_mesma_chave()
    {
        var api = Api();
        var console = new TestConsole();

        await new TelaDemo(console, SessaoDeTeste.Para(Perfis.Operador, api))
            .ExecutarAsync(TestContext.Current.CancellationToken);

        var lancamentos = api.Recebidas.Where(r => r.Metodo == HttpMethod.Post && !r.Url.AbsolutePath.EndsWith("/estornos")).ToList();
        Assert.Equal(2, lancamentos.Count);
        var estornos = api.Recebidas.Where(r => r.Url.AbsolutePath.EndsWith("/estornos")).ToList();
        Assert.Equal(2, estornos.Count);
        Assert.Equal(estornos[0].ChaveDeIdempotencia, estornos[1].ChaveDeIdempotencia);
        Assert.Contains("1.179,50", console.Output);
        Assert.Contains("idempotência confirmada", console.Output);
    }

    [Fact]
    [Trait("Criterio", "AC2")]
    public async Task Perfil_de_consulta_nao_roda_a_demo()
    {
        var api = Api();
        var console = new TestConsole();

        await new TelaDemo(console, SessaoDeTeste.Para(Perfis.Consulta, api))
            .ExecutarAsync(TestContext.Current.CancellationToken);

        Assert.Empty(api.Recebidas);
        Assert.Contains("operador", console.Output);
    }
}
