using Cashflow.Domain.Repositories;
using Cashflow.Modules.Lancamentos.Entities;

namespace Cashflow.Modules.Lancamentos.Repositories;

public interface ILancamentoRepository : IRepository<Lancamento, Guid>
{
    /// <summary>Lê o lançamento do comerciante travando a linha até o fim da transação.</summary>
    Task<Lancamento?> GetForUpdateAsync(Guid comercianteId, Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExisteEstornoDeAsync(Guid lancamentoId, CancellationToken cancellationToken = default);
}
