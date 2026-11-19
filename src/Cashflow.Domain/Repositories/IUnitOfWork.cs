using System.Data;

namespace Cashflow.Domain.Repositories;

/// <summary>
/// A transação do módulo. O caso de uso abre, os repositórios escrevem nela, o caso de uso confirma.
/// Sair do bloco <c>using</c> sem confirmar desfaz tudo: é o que garante "o lançamento e o evento
/// do outbox juntos, ou nenhum dos dois".
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Abre a transação do escopo. O retorno é descartável: descartar sem
    /// <see cref="CommitTransactionAsync"/> faz o rollback.
    /// </summary>
    Task<IDisposable> BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default);

    /// <summary>Confirma a transação aberta. Sem transação aberta é erro de programação, e lança.</summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
}
