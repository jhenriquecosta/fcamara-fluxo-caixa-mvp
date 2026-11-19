namespace Cashflow.Application;

/// <summary>
/// Executa um <see cref="ICommand"/>. Mora no mesmo arquivo do comando, como na referência, e recebe
/// os decorators que os atributos da classe pedem (<c>[AuditLog]</c>, <c>[DatabaseRetry]</c>).
/// </summary>
/// <typeparam name="TCommand">O comando executado.</typeparam>
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    /// <summary>Executa o caso de uso.</summary>
    Task HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
