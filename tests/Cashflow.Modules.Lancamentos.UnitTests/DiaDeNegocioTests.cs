using Cashflow.Modules.Lancamentos.Entities;

namespace Cashflow.Modules.Lancamentos.UnitTests;

/// <summary>O dia de um lançamento é o de São Paulo, não o do relógio UTC do servidor.</summary>
public sealed class DiaDeNegocioTests
{
    private static readonly TimeZoneInfo SaoPaulo = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    [Fact]
    public void Venda_as_23h30_em_Sao_Paulo_conta_no_mesmo_dia_mesmo_ja_sendo_o_dia_seguinte_em_UTC()
    {
        // Arrange: 02h30 de 18/11 em UTC é 23h30 de 17/11 em São Paulo (UTC-3)
        var instanteUtc = new DateTimeOffset(2026, 11, 18, 2, 30, 0, TimeSpan.Zero);

        // Act
        var dia = DiaDeNegocio.De(instanteUtc, SaoPaulo);

        // Assert
        Assert.Equal(new DateOnly(2026, 11, 17), dia);
    }

    [Fact]
    public void Venda_a_meia_noite_em_Sao_Paulo_ja_conta_no_dia_novo()
    {
        // Arrange: 03h00 de 18/11 em UTC é 00h00 de 18/11 em São Paulo
        var instanteUtc = new DateTimeOffset(2026, 11, 18, 3, 0, 0, TimeSpan.Zero);

        // Act
        var dia = DiaDeNegocio.De(instanteUtc, SaoPaulo);

        // Assert
        Assert.Equal(new DateOnly(2026, 11, 18), dia);
    }
}
