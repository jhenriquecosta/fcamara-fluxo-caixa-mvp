using Cashflow.Blazor.Modules.Core.Authorization;
using Spectre.Console.Testing;

namespace Cashflow.Terminal.Tests;

/// <summary>A escolha do perfil: o prompt aparece e as duas opções levam ao perfil certo.</summary>
public sealed class SelecaoDePerfilTests
{
    [Fact]
    [Trait("Criterio", "AC4")]
    public void Enter_mantem_o_operador()
    {
        var console = new TestConsole();
        console.Input.PushTextWithEnter("");

        Assert.Equal(Perfis.Operador, SelecaoDePerfil.Escolher(console));
    }

    [Fact]
    [Trait("Criterio", "AC4")]
    public void Dois_escolhe_o_perfil_de_consulta()
    {
        var console = new TestConsole();
        console.Input.PushTextWithEnter("2");

        Assert.Equal(Perfis.Consulta, SelecaoDePerfil.Escolher(console));
    }
}
