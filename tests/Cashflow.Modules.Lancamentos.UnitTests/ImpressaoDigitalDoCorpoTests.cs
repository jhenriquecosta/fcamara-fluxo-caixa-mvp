using Cashflow.Modules.Lancamentos.Commands;
using Cashflow.Modules.Lancamentos.Entities;

namespace Cashflow.Modules.Lancamentos.UnitTests;

/// <summary>
/// A impressão digital do corpo decide se uma chave de idempotência reusada é repetição (devolve a
/// resposta gravada) ou uso indevido (422).
/// </summary>
public sealed class ImpressaoDigitalDoCorpoTests
{
    private static string Impressao(TipoLancamento tipo, decimal valor, string? descricao = null) =>
        RegistrarLancamentoCommand.ImpressaoDigitalDoCorpo(tipo, valor, descricao);

    [Fact]
    [Trait("Criterio", "AC2")]
    public void O_mesmo_valor_escrito_de_outro_jeito_e_o_mesmo_pedido() =>
        Assert.Equal(Impressao(TipoLancamento.Debito, 100m), Impressao(TipoLancamento.Debito, 100.00m));

    [Fact]
    [Trait("Criterio", "AC3")]
    public void Outro_valor_e_outro_pedido() =>
        Assert.NotEqual(Impressao(TipoLancamento.Debito, 100m), Impressao(TipoLancamento.Debito, 200m));

    [Fact]
    [Trait("Criterio", "AC3")]
    public void Outro_tipo_e_outro_pedido() =>
        Assert.NotEqual(Impressao(TipoLancamento.Debito, 100m), Impressao(TipoLancamento.Credito, 100m));

    [Fact]
    [Trait("Criterio", "AC3")]
    public void Outra_descricao_e_outro_pedido() =>
        Assert.NotEqual(Impressao(TipoLancamento.Debito, 100m, "venda"), Impressao(TipoLancamento.Debito, 100m, "troco"));
}
