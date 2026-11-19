using Cashflow.Blazor.Modules.Core.Authorization;
using Spectre.Console.Testing;

namespace Cashflow.Terminal.Tests;

/// <summary>PRD-0004 RF5: o menu do terminal mostra só o que o perfil da sessão pode fazer.</summary>
public sealed class MenuPrincipalTests
{
    [Fact]
    [Trait("Criterio", "AC4")]
    public async Task Perfil_de_consulta_nao_ve_a_acao_de_lancar()
    {
        var console = new TestConsole();
        console.Input.PushTextWithEnter("0");

        await new MenuPrincipal(console, SessaoDeTeste.Para(Perfis.Consulta))
            .ExecutarAsync(TestContext.Current.CancellationToken);

        Assert.Contains("Consolidado", console.Output);
        Assert.Contains("Sair", console.Output);
        Assert.DoesNotContain("Lançar", console.Output);
    }

    [Fact]
    [Trait("Criterio", "AC4")]
    public async Task Perfil_operador_ve_a_acao_de_lancar()
    {
        var console = new TestConsole();
        console.Input.PushTextWithEnter("0");

        await new MenuPrincipal(console, SessaoDeTeste.Para(Perfis.Operador))
            .ExecutarAsync(TestContext.Current.CancellationToken);

        Assert.Contains("Lançar", console.Output);
        Assert.Contains("Sair", console.Output);
    }
}
