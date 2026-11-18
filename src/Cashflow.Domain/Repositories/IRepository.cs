namespace Cashflow.Domain.Repositories;

public interface IConcurrencyHandler<TEntity>
{
}

/// <summary>
/// A forma do repositório da referência. Os módulos usam a porta própria
/// (ILancamentoRepository, IConsolidadoRepository) por causa do Dapper — a
/// divergência declarada no ADR-0010.
/// </summary>
public interface IRepository<TEntity, TKey> : IConcurrencyHandler<TEntity>
    where TEntity : Entities.Entity<TKey>, Entities.IAggregateRoot
{
    IUnitOfWork UnitOfWork { get; }

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    void Delete(TEntity entity);
}
