using Cashflow.Domain.Repositories;
using Cashflow.Modules.Lancamentos.Entities;

namespace Cashflow.Modules.Lancamentos.Repositories;

/// <summary>O livro-caixa do comerciante: onde os lançamentos são gravados e de onde o estorno lê o original.</summary>
public interface ILancamentoRepository : IRepository<Lancamento, Guid>
{
    /// <summary>Lê o lançamento do comerciante travando a linha até o fim da transação.</summary>
    Task<Lancamento?> GetForUpdateAsync(Guid comercianteId, Guid id, CancellationToken cancellationToken = default);

    /// <summary>Diz se o lançamento já tem estorno. É a checagem da regra "estorna uma vez só".</summary>
    Task<bool> ExisteEstornoDeAsync(Guid lancamentoId, CancellationToken cancellationToken = default);
}
