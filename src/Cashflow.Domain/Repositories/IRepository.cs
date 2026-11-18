using Cashflow.Domain.Entities;

namespace Cashflow.Domain.Repositories;

/// <summary>
/// O repositório da referência, sem o <c>IQueryable</c>: com Dapper (ADR-0006) a consulta é
/// SQL explícito no repositório específico do módulo, que estende esta interface.
/// </summary>
public interface IRepository<TEntity, TKey> : IConcurrencyHandler<TEntity>
    where TEntity : Entity<TKey>, IAggregateRoot
{
    IUnitOfWork UnitOfWork { get; }

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
}
