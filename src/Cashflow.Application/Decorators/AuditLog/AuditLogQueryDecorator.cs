using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Cashflow.Application.Decorators.AuditLog;

[Mapping(Type = typeof(AuditLogAttribute))]
public sealed class AuditLogQueryDecorator<TQuery, TResult>(
    IQueryHandler<TQuery, TResult> handler,
    ILogger<AuditLogQueryDecorator<TQuery, TResult>> logger) : IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    public async Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default)
    {
        var inicio = Stopwatch.GetTimestamp();
        var resultado = await handler.HandleAsync(query, cancellationToken);
        logger.LogDebug("consulta {Query} executada em {ElapsedMs:0.0} ms",
            typeof(TQuery).Name, Stopwatch.GetElapsedTime(inicio).TotalMilliseconds);
        return resultado;
    }
}
