using System.Data;
using Cashflow.Domain.Repositories;
using Npgsql;

namespace Cashflow.Infrastructure.Persistence;

/// <summary>
/// O papel do <c>DbContext</c> da referência sobre Dapper (ADR-0006): uma conexão por escopo e a
/// transação que os repositórios do módulo compartilham. Cada módulo herda o seu, com o banco dele.
/// </summary>
public abstract class DbContext(NpgsqlDataSource dataSource) : IUnitOfWork, IAsyncDisposable, IDisposable
{
    private NpgsqlConnection? _connection;

    public NpgsqlTransaction? CurrentTransaction { get; private set; }

    public async Task<NpgsqlConnection> GetConnectionAsync(CancellationToken cancellationToken = default) =>
        _connection ??= await dataSource.OpenConnectionAsync(cancellationToken);

    public async Task<IDisposable> BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default)
    {
        if (CurrentTransaction is not null)
            throw new InvalidOperationException("já existe uma transação aberta neste escopo");

        var connection = await GetConnectionAsync(cancellationToken);
        CurrentTransaction = await connection.BeginTransactionAsync(isolationLevel, cancellationToken);
        return new TransactionScope(this);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentTransaction is null)
            throw new InvalidOperationException("nenhuma transação aberta para confirmar");

        await CurrentTransaction.CommitAsync(cancellationToken);
        await EncerrarTransacaoAsync();
    }

    /// <summary>O Dapper grava na hora; aqui o "salvar" confirma a transação aberta, se houver.</summary>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentTransaction is not null)
            await CommitTransactionAsync(cancellationToken);
        return 0;
    }

    private async ValueTask EncerrarTransacaoAsync()
    {
        if (CurrentTransaction is null)
            return;

        await CurrentTransaction.DisposeAsync();
        CurrentTransaction = null;
    }

    public async ValueTask DisposeAsync()
    {
        await EncerrarTransacaoAsync();
        if (_connection is not null)
            await _connection.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    public void Dispose()
    {
        CurrentTransaction?.Dispose();
        CurrentTransaction = null;
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>Sair do escopo sem confirmar desfaz a transação.</summary>
    private sealed class TransactionScope(DbContext context) : IDisposable
    {
        public void Dispose() => context.EncerrarTransacaoAsync().AsTask().GetAwaiter().GetResult();
    }
}
