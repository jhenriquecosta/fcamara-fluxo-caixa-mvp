using Cashflow.Modules.Lancamentos.Commands;
using Cashflow.Modules.Lancamentos.Entities;

namespace Cashflow.Modules.Lancamentos.UnitTests;

public sealed class DiaDeNegocioTests
{
    private static readonly TimeZoneInfo SaoPaulo = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    [Fact]
    public void Madrugada_UTC_ainda_e_o_dia_anterior_em_Sao_Paulo() =>
        Assert.Equal(new DateOnly(2026, 11, 17),
            DiaDeNegocio.De(new DateTimeOffset(2026, 11, 18, 2, 30, 0, TimeSpan.Zero), SaoPaulo));

    [Theory]
    [InlineData("credito", TipoLancamento.Credito)]
    [InlineData("DEBITO", TipoLancamento.Debito)]
    public void Tipo_e_lido_sem_diferenciar_caixa(string texto, TipoLancamento esperado)
    {
        Assert.True(TipoLancamentoExtensions.TryParseTexto(texto, out var tipo));
        Assert.Equal(esperado, tipo);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalido")]
    [InlineData("1")]
    public void Tipo_fora_do_dominio_e_recusado(string? texto) =>
        Assert.False(TipoLancamentoExtensions.TryParseTexto(texto, out _));

    [Fact]
    public void Hash_do_corpo_distingue_valor_e_tipo_e_e_estavel()
    {
        var hash = RegistrarLancamentoCommand.HashDoCorpo(TipoLancamento.Debito, 100m, null);

        Assert.Equal(hash, RegistrarLancamentoCommand.HashDoCorpo(TipoLancamento.Debito, 100.00m, null));
        Assert.NotEqual(hash, RegistrarLancamentoCommand.HashDoCorpo(TipoLancamento.Debito, 200m, null));
        Assert.NotEqual(hash, RegistrarLancamentoCommand.HashDoCorpo(TipoLancamento.Credito, 100m, null));
    }
}
