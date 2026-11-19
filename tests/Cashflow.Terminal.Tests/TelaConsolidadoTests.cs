using Cashflow.Blazor.Modules.Core.Authorization;
using Spectre.Console.Testing;

namespace Cashflow.Terminal.Tests;

/// <summary>PRD-0004 RF5/AC4/AC5: o consolidado aparece com os três saldos, e a queda não derruba a tela.</summary>
public sealed class TelaConsolidadoTests
{
    [Fact]
    [Trait("Criterio", "AC4")]
    public async Task Mostra_saldo_do_dia_anterior_e_acumulado()
    {
        var api = new ApiFalsa(_ => ApiFalsa.Json(
            """{"comercianteId":"22222222-2222-2222-2222-222222222222","data":"2026-11-19","saldoDia":-240.50,"saldoAnterior":0,"saldoAcumulado":-240.50}"""));
        var console = new TestConsole();

        await new TelaConsolidado(console, SessaoDeTeste.Para(Perfis.Consulta, api))
            .ExecutarAsync(TestContext.Current.CancellationToken);

        Assert.Contains("240,50", console.Output);
        Assert.Contains("anterior", console.Output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("acumulado", console.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Criterio", "AC5")]
    public async Task Servico_fora_aparece_indisponivel_sem_derrubar_a_tela()
    {
        var api = new ApiFalsa(_ => throw new HttpRequestException("conexão recusada"));
        var console = new TestConsole();

        await new TelaConsolidado(console, SessaoDeTeste.Para(Perfis.Operador, api))
            .ExecutarAsync(TestContext.Current.CancellationToken);

        Assert.Contains("indisponível", console.Output, StringComparison.OrdinalIgnoreCase);
    }
}
