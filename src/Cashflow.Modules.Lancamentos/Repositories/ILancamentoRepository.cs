using Cashflow.Domain.Repositories;
using Cashflow.Modules.Lancamentos.Entities;
using Cashflow.Modules.Lancamentos.Models;

namespace Cashflow.Modules.Lancamentos.Repositories;

public interface ILancamentoRepository : IRepository<Lancamento, Guid>
{
    /// <summary>Lê o lançamento do comerciante travando a linha até o fim da transação.</summary>
    Task<Lancamento?> GetForUpdateAsync(Guid comercianteId, Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExisteEstornoDeAsync(Guid lancamentoId, CancellationToken cancellationToken = default);

    /// <summary>Os lançamentos do comerciante no dia, em ordem de criação, marcando os que já têm estorno.</summary>
    Task<IReadOnlyList<LancamentoDoDiaModel>> ListarDoDiaAsync(
        Guid comercianteId, DateOnly data, CancellationToken cancellationToken = default);
}
