using Cashflow.Modules.Lancamentos.Entities;

namespace Cashflow.Modules.Lancamentos.UnitTests;

public sealed class LancamentoTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 18, 2, 30, 0, TimeSpan.Zero);
    private static readonly DateOnly Dia = new(2026, 11, 17);

    [Fact]
    public void Registrar_cria_o_lancamento_com_o_dia_de_negocio()
    {
        var comerciante = Guid.NewGuid();

        var lancamento = Lancamento.Registrar(comerciante, TipoLancamento.Credito, 100m, "venda", Dia, Agora);

        Assert.NotEqual(Guid.Empty, lancamento.Id);
        Assert.Equal(comerciante, lancamento.ComercianteId);
        Assert.Equal(TipoLancamento.Credito, lancamento.Tipo);
        Assert.Equal(100m, lancamento.Valor);
        Assert.Equal(Dia, lancamento.Data);
        Assert.Equal(Agora, lancamento.CreatedDateTime);
        Assert.False(lancamento.EhEstorno);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Registrar_recusa_valor_que_nao_e_positivo(decimal valor) =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Lancamento.Registrar(Guid.NewGuid(), TipoLancamento.Debito, valor, null, Dia, Agora));

    [Fact]
    public void Estornar_cria_o_contra_lancamento_de_tipo_oposto_e_preserva_o_original()
    {
        var original = Lancamento.Registrar(Guid.NewGuid(), TipoLancamento.Credito, 100m, "venda", Dia, Agora);

        var estorno = Lancamento.Estornar(original, Dia, Agora);

        Assert.NotEqual(original.Id, estorno.Id);
        Assert.Equal(TipoLancamento.Debito, estorno.Tipo);
        Assert.Equal(original.Valor, estorno.Valor);
        Assert.Equal(original.Id, estorno.EstornoDe);
        Assert.Null(estorno.Descricao);
        Assert.Equal(TipoLancamento.Credito, original.Tipo);
        Assert.Null(original.EstornoDe);
    }

    [Fact]
    public void Estorno_nao_pode_ser_estornado()
    {
        var original = Lancamento.Registrar(Guid.NewGuid(), TipoLancamento.Debito, 40m, null, Dia, Agora);
        var estorno = Lancamento.Estornar(original, Dia, Agora);

        Assert.Throws<LancamentoNaoPodeSerEstornadoException>(() => Lancamento.Estornar(estorno, Dia, Agora));
    }
}
