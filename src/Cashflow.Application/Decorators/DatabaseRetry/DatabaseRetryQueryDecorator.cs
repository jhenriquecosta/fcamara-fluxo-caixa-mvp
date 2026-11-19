namespace Cashflow.Application.Decorators.DatabaseRetry;

[Mapping(Type = typeof(DatabaseRetryAttribute))]
public sealed class DatabaseRetryQueryDecorator<TQuery, TResult>(
    IQueryHandler<TQuery, TResult> handler,
    DatabaseRetryAttribute options) : DatabaseRetryDecoratorBase(options), IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    public Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default) =>
        WrapExecutionAsync(() => handler.HandleAsync(query, cancellationToken));
}
