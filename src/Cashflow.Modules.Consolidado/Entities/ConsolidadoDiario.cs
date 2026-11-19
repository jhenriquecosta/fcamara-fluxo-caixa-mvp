namespace Cashflow.Modules.Consolidado.Entities;

/// <summary>
/// O saldo de um dia do caixa de um comerciante (RF-03). É um modelo de leitura alimentado por eventos
/// (projeção, ADR-0005), não um agregado: não nasce de comando nem tem invariante além do saldo acumulado.
/// </summary>
/// <param name="ComercianteId">O dono do caixa.</param>
/// <param name="Data">O dia de negócio.</param>
/// <param name="SaldoDia">Créditos menos débitos do próprio dia.</param>
/// <param name="SaldoAcumulado">O saldo ao fim do dia: o saldo de abertura mais o do dia.</param>
public sealed record ConsolidadoDiario(Guid ComercianteId, DateOnly Data, decimal SaldoDia, decimal SaldoAcumulado)
{
    /// <summary>O saldo com que o dia abriu: o acumulado do último dia anterior com movimento.</summary>
    public decimal SaldoAnterior => SaldoAcumulado - SaldoDia;

    /// <summary>
    /// Um dia em que nada foi lançado: saldo do dia zero e o acumulado igual ao de abertura. É a resposta
    /// para qualquer data sem movimento, em vez de 404: o caixa existe todo dia.
    /// </summary>
    public static ConsolidadoDiario SemMovimento(Guid comercianteId, DateOnly data, decimal saldoDeAbertura) =>
        new(comercianteId, data, 0m, saldoDeAbertura);
}
