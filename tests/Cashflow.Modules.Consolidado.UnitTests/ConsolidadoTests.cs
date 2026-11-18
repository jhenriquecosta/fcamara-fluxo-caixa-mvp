using Cashflow.Domain.Repositories;
using Cashflow.Modules.Consolidado.Entities;
using Cashflow.Modules.Consolidado.Queries;
using Cashflow.Modules.Consolidado.Repositories;

namespace Cashflow.Modules.Consolidado.UnitTests;

public sealed class ConsolidadoTests
{
    private static readonly DateOnly Dia = new(2026, 11, 17);

    [Theory]
    [InlineData("credito", 70, 70)]
    [InlineData("debito", 70, -70)]
    public void Credito_soma_e_debito_subtrai(string tipo, decimal valor, decimal delta) =>
        Assert.Equal(delta, new MovimentoDoDia(Guid.NewGuid(), Guid.NewGuid(), tipo, valor, Dia).Delta);

    [Fact]
    public void Saldo_anterior_e_o_acumulado_menos_o_dia() =>
        Assert.Equal(30m, new ConsolidadoDiario(Guid.NewGuid(), Dia, 20m, 50m).SaldoAnterior);

    [Fact]
    public async Task Dia_sem_movimento_responde_zero_com_o_acumulado_anterior()
    {
        var comerciante = Guid.NewGuid();
        var handler = new GetConsolidadoDiarioQueryHandler(new RepositorioFalso(saldoAnterior: 120m));

        var modelo = await handler.HandleAsync(
            new GetConsolidadoDiarioQuery { ComercianteId = comerciante, Data = Dia },
            TestContext.Current.CancellationToken);

        Assert.Equal(comerciante, modelo.ComercianteId);
        Assert.Equal(0m, modelo.SaldoDia);
        Assert.Equal(120m, modelo.SaldoAnterior);
        Assert.Equal(120m, modelo.SaldoAcumulado);
    }

    private sealed class RepositorioFalso(decimal saldoAnterior) : IConsolidadoRepository
    {
        public IUnitOfWork UnitOfWork => throw new NotSupportedException();

        public Task<ConsolidadoDiario?> GetAsync(Guid comercianteId, DateOnly data, CancellationToken cancellationToken = default) =>
            Task.FromResult<ConsolidadoDiario?>(null);

        public Task<decimal> SaldoAnteriorAsync(Guid comercianteId, DateOnly data, CancellationToken cancellationToken = default) =>
            Task.FromResult(saldoAnterior);

        public Task<ProjecaoResultado> AplicarAsync(MovimentoDoDia movimento, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
