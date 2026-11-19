using Cashflow.Domain.Repositories;
using Cashflow.Modules.Consolidado.Entities;

namespace Cashflow.Modules.Consolidado.Repositories;

/// <summary>O que aconteceu ao projetar um movimento.</summary>
public enum ResultadoDaProjecao
{
    /// <summary>O movimento entrou no saldo do dia.</summary>
    Somado,

    /// <summary>O evento já tinha sido somado antes (o Kafka entrega at-least-once) e foi ignorado.</summary>
    EventoJaProcessado
}

/// <summary>Os saldos diários projetados, na tabela <c>consolidado_diario</c>.</summary>
public interface IConsolidadoRepository
{
    /// <summary>A unidade de trabalho do módulo.</summary>
    IUnitOfWork UnitOfWork { get; }

    /// <summary>O consolidado do dia, ou <c>null</c> se o comerciante não teve movimento nele.</summary>
    Task<ConsolidadoDiario?> ObterDoDiaAsync(Guid comercianteId, DateOnly data, CancellationToken cancellationToken = default);

    /// <summary>O saldo com que o dia abre: o acumulado do último dia anterior com movimento, ou zero.</summary>
    Task<decimal> SaldoDeAberturaAsync(Guid comercianteId, DateOnly data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registra o evento como processado e soma o movimento no mesmo passo. Um evento repetido volta
    /// <see cref="ResultadoDaProjecao.EventoJaProcessado"/> e não soma de novo.
    /// </summary>
    Task<ResultadoDaProjecao> ProjetarAsync(MovimentoDoDia movimento, CancellationToken cancellationToken = default);
}
