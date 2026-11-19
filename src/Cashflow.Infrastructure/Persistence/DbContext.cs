using System.Data;
using Cashflow.Domain.Repositories;
using Npgsql;

namespace Cashflow.Infrastructure.Persistence;

/// <summary>
/// O papel do <c>DbContext</c> da referência sobre Dapper (ADR-0006): uma conexão por escopo de DI e a
/// transação que os repositórios do módulo compartilham. Cada módulo herda o seu, apontando para o banco dele.
/// </summary>
/// <remarks>
/// Os repositórios pegam a conexão em <see cref="GetConnectionAsync"/> e passam
/// <see cref="CurrentTransaction"/> a cada comando: é assim que escritas de repositórios diferentes
/// caem na mesma transação.
/// </remarks>
public abstract class DbContext(NpgsqlDataSource dataSource) : IUnitOfWork, IAsyncDisposable, IDisposable
{
    private NpgsqlConnection? _connection;

    /// <summary>A transação aberta neste escopo, ou <c>null</c> fora de uma.</summary>
    public NpgsqlTransaction? CurrentTransaction { get; private set; }

    /// <summary>A conexão do escopo, aberta na primeira chamada e reaproveitada nas seguintes.</summary>
    public async Task<NpgsqlConnection> GetConnectionAsync(CancellationToken cancellationToken = default) =>
        _connection ??= await dataSource.OpenConnectionAsync(cancellationToken);

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Já existe uma transação aberta: transações aninhadas não são suportadas.</exception>
    public async Task<IDisposable> BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default)
    {
        if (CurrentTransaction is not null)
            throw new InvalidOperationException("já existe uma transação aberta neste escopo");

        var connection = await GetConnectionAsync(cancellationToken);
        CurrentTransaction = await connection.BeginTransactionAsync(isolationLevel, cancellationToken);
        return new RollbackUnlessCommitted(this);
    }

    /// <inheritdoc />
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentTransaction is null)
            throw new InvalidOperationException("nenhuma transação aberta para confirmar");

        await CurrentTransaction.CommitAsync(cancellationToken);
        await EndTransactionAsync();
    }

    /// <summary>Descarta a transação. Se ainda não foi confirmada, o descarte é o rollback.</summary>
    private async ValueTask EndTransactionAsync()
    {
        if (CurrentTransaction is null)
            return;

        await CurrentTransaction.DisposeAsync();
        CurrentTransaction = null;
    }

    /// <summary>Encerra a transação pendente (desfazendo-a) e devolve a conexão ao pool.</summary>
    public async ValueTask DisposeAsync()
    {
        await EndTransactionAsync();
        if (_connection is not null)
            await _connection.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc cref="DisposeAsync"/>
    public void Dispose()
    {
        CurrentTransaction?.Dispose();
        CurrentTransaction = null;
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>O que <see cref="BeginTransactionAsync"/> devolve: sair do <c>using</c> sem confirmar desfaz a transação.</summary>
    private sealed class RollbackUnlessCommitted(DbContext context) : IDisposable
    {
        public void Dispose() => context.EndTransactionAsync().AsTask().GetAwaiter().GetResult();
    }
}
