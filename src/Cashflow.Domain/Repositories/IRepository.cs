using Cashflow.Domain.Entities;

namespace Cashflow.Domain.Repositories;

/// <summary>
/// O repositório genérico da referência, sem <c>IQueryable</c>: com Dapper (ADR-0006) cada consulta
/// é SQL explícito no repositório do módulo, que estende esta interface com o que o caso de uso precisa.
/// </summary>
/// <typeparam name="TEntity">A raiz de agregado que o repositório guarda.</typeparam>
/// <typeparam name="TKey">O tipo da identidade.</typeparam>
public interface IRepository<TEntity, TKey>
    where TEntity : Entity<TKey>, IAggregateRoot
{
    /// <summary>A unidade de trabalho do módulo: abre e confirma a transação que os repositórios dele compartilham.</summary>
    IUnitOfWork UnitOfWork { get; }

    /// <summary>Grava a raiz nova na transação aberta, se houver.</summary>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>Carrega a raiz pela identidade, ou <c>null</c> se ela não existe.</summary>
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
}
