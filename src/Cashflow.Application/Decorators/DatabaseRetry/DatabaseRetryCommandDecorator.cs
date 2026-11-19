namespace Cashflow.Application.Decorators.DatabaseRetry;

/// <summary>Repete o comando quando o banco devolve uma falha transitória.</summary>
[Mapping(Type = typeof(DatabaseRetryAttribute))]
public sealed class DatabaseRetryCommandDecorator<TCommand>(
    ICommandHandler<TCommand> handler,
    DatabaseRetryAttribute options) : DatabaseRetryDecoratorBase(options), ICommandHandler<TCommand>
    where TCommand : ICommand
{
    /// <inheritdoc />
    public Task HandleAsync(TCommand command, CancellationToken cancellationToken = default) =>
        ExecuteWithRetryAsync(() => handler.HandleAsync(command, cancellationToken));
}
