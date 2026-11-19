namespace Cashflow.Application.Decorators.DatabaseRetry;

/// <summary>Repete a consulta quando o banco devolve uma falha transitória.</summary>
[Mapping(Type = typeof(DatabaseRetryAttribute))]
public sealed class DatabaseRetryQueryDecorator<TQuery, TResult>(
    IQueryHandler<TQuery, TResult> handler,
    DatabaseRetryAttribute options) : DatabaseRetryDecoratorBase(options), IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    /// <inheritdoc />
    public Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default) =>
        ExecuteWithRetryAsync(() => handler.HandleAsync(query, cancellationToken));
}
