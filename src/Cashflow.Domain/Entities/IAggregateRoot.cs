namespace Cashflow.Domain.Entities;

/// <summary>
/// Marca a raiz de um agregado (DDD): a única entidade que um repositório carrega e grava, e a
/// fronteira da consistência transacional. Só uma raiz pode originar um <c>EntityCreatedEvent</c>.
/// </summary>
public interface IAggregateRoot;
