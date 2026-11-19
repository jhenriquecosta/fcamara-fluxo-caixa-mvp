namespace Cashflow.Application.Decorators.DatabaseRetry;

[Mapping(Type = typeof(DatabaseRetryAttribute))]
public sealed class DatabaseRetryCommandDecorator<TCommand>(
    ICommandHandler<TCommand> handler,
    DatabaseRetryAttribute options) : DatabaseRetryDecoratorBase(options), ICommandHandler<TCommand>
    where TCommand : ICommand
{
    public Task HandleAsync(TCommand command, CancellationToken cancellationToken = default) =>
        WrapExecutionAsync(() => handler.HandleAsync(command, cancellationToken));
}
