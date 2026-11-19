using Cashflow.Domain.Repositories;
using Cashflow.Modules.Lancamentos.Entities;
using Cashflow.Modules.Lancamentos.Models;

namespace Cashflow.Modules.Lancamentos.Repositories;

/// <summary>O livro-caixa do comerciante: onde os lançamentos são gravados e de onde o estorno lê o original.</summary>
public interface ILancamentoRepository : IRepository<Lancamento, Guid>
{
    /// <summary>Lê o lançamento do comerciante travando a linha até o fim da transação.</summary>
    Task<Lancamento?> GetForUpdateAsync(Guid comercianteId, Guid id, CancellationToken cancellationToken = default);

    /// <summary>Diz se o lançamento já tem estorno. É a checagem da regra "estorna uma vez só".</summary>
    Task<bool> ExisteEstornoDeAsync(Guid lancamentoId, CancellationToken cancellationToken = default);

    /// <summary>Os lançamentos do comerciante no dia, em ordem de criação, marcando os que já têm estorno.</summary>
    Task<IReadOnlyList<LancamentoDoDiaModel>> ListarDoDiaAsync(
        Guid comercianteId, DateOnly data, CancellationToken cancellationToken = default);
}
