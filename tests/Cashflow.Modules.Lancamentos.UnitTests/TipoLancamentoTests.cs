using Cashflow.Modules.Lancamentos.Entities;

namespace Cashflow.Modules.Lancamentos.UnitTests;

/// <summary>O tipo no contrato da API é <c>debito</c> ou <c>credito</c>, e nada mais (ADR-0009).</summary>
public sealed class TipoLancamentoTests
{
    [Theory]
    [InlineData("credito", TipoLancamento.Credito)]
    [InlineData("CREDITO", TipoLancamento.Credito)]
    [InlineData("debito", TipoLancamento.Debito)]
    [InlineData("Debito", TipoLancamento.Debito)]
    public void Le_o_tipo_do_contrato_sem_diferenciar_maiusculas(string texto, TipoLancamento esperado)
    {
        var lido = TipoLancamentoExtensions.TryLerDoContrato(texto, out var tipo);

        Assert.True(lido);
        Assert.Equal(esperado, tipo);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalido")]
    [InlineData("0")]
    [InlineData("1")]
    public void Recusa_texto_fora_do_contrato_inclusive_os_numeros_que_o_Enum_TryParse_aceitaria(string? texto) =>
        Assert.False(TipoLancamentoExtensions.TryLerDoContrato(texto, out _));

    [Theory]
    [InlineData(TipoLancamento.Credito, TipoLancamento.Debito)]
    [InlineData(TipoLancamento.Debito, TipoLancamento.Credito)]
    public void O_oposto_de_um_tipo_e_o_tipo_do_seu_estorno(TipoLancamento tipo, TipoLancamento oposto) =>
        Assert.Equal(oposto, tipo.Oposto());
}
