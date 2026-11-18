using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Cashflow.Application.Decorators.AuditLog;

/// <summary>
/// Registra o comando executado e quanto levou. Só o tipo: o corpo carrega o comerciante,
/// que nos logs aparece apenas como hash (docs/ultron/agents/logging.md).
/// </summary>
[Mapping(Type = typeof(AuditLogAttribute))]
public sealed class AuditLogCommandDecorator<TCommand>(
    ICommandHandler<TCommand> handler,
    ILogger<AuditLogCommandDecorator<TCommand>> logger) : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    public async Task HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        var inicio = Stopwatch.GetTimestamp();
        await handler.HandleAsync(command, cancellationToken);
        logger.LogInformation("comando {Command} executado em {ElapsedMs:0.0} ms",
            typeof(TCommand).Name, Stopwatch.GetElapsedTime(inicio).TotalMilliseconds);
    }
}
