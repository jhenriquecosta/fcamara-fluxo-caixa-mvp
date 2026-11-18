using Cashflow.Domain.Repositories;
using Cashflow.Modules.Consolidado.Entities;

namespace Cashflow.Modules.Consolidado.Repositories;

public enum ProjecaoResultado
{
    Aplicado,
    Duplicado
}

public interface IConsolidadoRepository
{
    IUnitOfWork UnitOfWork { get; }

    Task<ConsolidadoDiario?> GetAsync(Guid comercianteId, DateOnly data, CancellationToken cancellationToken = default);

    Task<decimal> SaldoAnteriorAsync(Guid comercianteId, DateOnly data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registra o evento como processado e soma o movimento no mesmo passo. Um evento repetido
    /// (at-least-once do Kafka) volta <see cref="ProjecaoResultado.Duplicado"/> e não soma de novo.
    /// </summary>
    Task<ProjecaoResultado> AplicarAsync(MovimentoDoDia movimento, CancellationToken cancellationToken = default);
}
