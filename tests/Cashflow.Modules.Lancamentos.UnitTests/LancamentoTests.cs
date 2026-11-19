using Cashflow.Modules.Lancamentos.Entities;

namespace Cashflow.Modules.Lancamentos.UnitTests;

/// <summary>
/// As regras do agregado <see cref="Lancamento"/>: o lançamento nasce válido, é imutável, e a correção
/// é um contra-lançamento que não toca no original.
/// </summary>
public sealed class LancamentoTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 18, 2, 30, 0, TimeSpan.Zero);
    private static readonly DateOnly DiaDeNegocio = new(2026, 11, 17);

    [Fact]
    [Trait("Criterio", "AC1")]
    public void Registrar_grava_o_comerciante_o_tipo_o_valor_e_o_dia_de_negocio_informados()
    {
        // Arrange
        var comerciante = Guid.NewGuid();

        // Act
        var lancamento = Lancamento.Registrar(comerciante, TipoLancamento.Credito, 100m, "venda", DiaDeNegocio, Agora);

        // Assert
        Assert.NotEqual(Guid.Empty, lancamento.Id);
        Assert.Equal(comerciante, lancamento.ComercianteId);
        Assert.Equal(TipoLancamento.Credito, lancamento.Tipo);
        Assert.Equal(100m, lancamento.Valor);
        Assert.Equal("venda", lancamento.Descricao);
        Assert.Equal(DiaDeNegocio, lancamento.Data);
        Assert.Equal(Agora, lancamento.CreatedDateTime);
        Assert.False(lancamento.EhEstorno);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void Registrar_recusa_valor_zero_ou_negativo_porque_o_sinal_vem_do_tipo(decimal valorInvalido) =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Lancamento.Registrar(Guid.NewGuid(), TipoLancamento.Debito, valorInvalido, null, DiaDeNegocio, Agora));

    [Fact]
    [Trait("Criterio", "AC4")]
    public void Estornar_cria_um_contra_lancamento_de_mesmo_valor_e_tipo_oposto_apontando_para_o_original()
    {
        // Arrange
        var original = Lancamento.Registrar(Guid.NewGuid(), TipoLancamento.Credito, 100m, "venda", DiaDeNegocio, Agora);

        // Act
        var estorno = Lancamento.Estornar(original, DiaDeNegocio, Agora);

        // Assert
        Assert.NotEqual(original.Id, estorno.Id);
        Assert.Equal(original.ComercianteId, estorno.ComercianteId);
        Assert.Equal(TipoLancamento.Debito, estorno.Tipo);
        Assert.Equal(original.Valor, estorno.Valor);
        Assert.Equal(original.Id, estorno.EstornoDe);
        Assert.True(estorno.EhEstorno);
        Assert.Null(estorno.Descricao);
    }

    [Fact]
    [Trait("Criterio", "AC4")]
    public void Estornar_nao_altera_o_lancamento_original()
    {
        // Arrange
        var original = Lancamento.Registrar(Guid.NewGuid(), TipoLancamento.Credito, 100m, "venda", DiaDeNegocio, Agora);

        // Act
        _ = Lancamento.Estornar(original, DiaDeNegocio, Agora);

        // Assert
        Assert.Equal(TipoLancamento.Credito, original.Tipo);
        Assert.Equal(100m, original.Valor);
        Assert.Null(original.EstornoDe);
    }

    [Fact]
    [Trait("Criterio", "AC5")]
    public void Estornar_um_estorno_e_recusado_porque_a_correcao_de_um_estorno_e_um_lancamento_novo()
    {
        // Arrange
        var original = Lancamento.Registrar(Guid.NewGuid(), TipoLancamento.Debito, 40m, null, DiaDeNegocio, Agora);
        var estorno = Lancamento.Estornar(original, DiaDeNegocio, Agora);

        // Act + Assert
        Assert.Throws<LancamentoNaoPodeSerEstornadoException>(() => Lancamento.Estornar(estorno, DiaDeNegocio, Agora));
    }
}
