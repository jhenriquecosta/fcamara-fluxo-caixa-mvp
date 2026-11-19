using Cashflow.Domain.Repositories;
using Cashflow.Modules.Consolidado.Entities;
using Cashflow.Modules.Consolidado.Queries;
using Cashflow.Modules.Consolidado.Repositories;

namespace Cashflow.Modules.Consolidado.UnitTests;

/// <summary>Como um movimento mexe no saldo e como se lê o saldo de um dia.</summary>
public sealed class ConsolidadoTests
{
    private static readonly DateOnly Dia = new(2026, 11, 17);

    [Theory]
    [InlineData("credito", 70, 70)]
    [InlineData("debito", 70, -70)]
    public void Credito_soma_ao_saldo_e_debito_subtrai(string tipo, decimal valor, decimal efeitoEsperado)
    {
        var movimento = new MovimentoDoDia(Guid.NewGuid(), Guid.NewGuid(), tipo, valor, Dia);

        Assert.Equal(efeitoEsperado, movimento.EfeitoNoSaldo);
    }

    [Fact]
    [Trait("Criterio", "AC4")]
    public void Um_estorno_anula_o_original_no_saldo_porque_chega_com_o_tipo_oposto()
    {
        var credito = new MovimentoDoDia(Guid.NewGuid(), Guid.NewGuid(), "credito", 50m, Dia);
        var estorno = new MovimentoDoDia(Guid.NewGuid(), credito.ComercianteId, "debito", 50m, Dia);

        Assert.Equal(0m, credito.EfeitoNoSaldo + estorno.EfeitoNoSaldo);
    }

    [Fact]
    public void O_saldo_de_abertura_e_o_acumulado_menos_o_movimento_do_dia()
    {
        var consolidado = new ConsolidadoDiario(Guid.NewGuid(), Dia, SaldoDia: 20m, SaldoAcumulado: 50m);

        Assert.Equal(30m, consolidado.SaldoAnterior);
    }

    [Fact]
    public async Task Dia_sem_movimento_responde_saldo_do_dia_zero_e_carrega_o_acumulado_do_dia_anterior()
    {
        // Arrange: nenhum consolidado no dia; o último dia anterior fechou com 120
        var comerciante = Guid.NewGuid();
        var handler = new ConsultarConsolidadoDoDiaQueryHandler(new RepositorioSemMovimentoNoDia(saldoDeAbertura: 120m));

        // Act
        var consolidado = await handler.HandleAsync(
            new ConsultarConsolidadoDoDiaQuery { ComercianteId = comerciante, Data = Dia },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(comerciante, consolidado.ComercianteId);
        Assert.Equal(0m, consolidado.SaldoDia);
        Assert.Equal(120m, consolidado.SaldoAnterior);
        Assert.Equal(120m, consolidado.SaldoAcumulado);
    }

    /// <summary>Um repositório em que o dia consultado não teve movimento.</summary>
    private sealed class RepositorioSemMovimentoNoDia(decimal saldoDeAbertura) : IConsolidadoRepository
    {
        public IUnitOfWork UnitOfWork => throw new NotSupportedException();

        public Task<ConsolidadoDiario?> ObterDoDiaAsync(Guid comercianteId, DateOnly data, CancellationToken cancellationToken = default) =>
            Task.FromResult<ConsolidadoDiario?>(null);

        public Task<decimal> SaldoDeAberturaAsync(Guid comercianteId, DateOnly data, CancellationToken cancellationToken = default) =>
            Task.FromResult(saldoDeAbertura);

        public Task<ResultadoDaProjecao> ProjetarAsync(MovimentoDoDia movimento, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
