using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Context;

namespace Cashflow.Infrastructure.Web;

/// <summary>O pipeline HTTP comum: log de requisição, correlação e segurança, na mesma ordem em todo host.</summary>
public static class CashflowWeb
{
    /// <summary>Log de cada requisição, contexto de correlação, autenticação e autorização.</summary>
    public static WebApplication UseCashflowWeb(this WebApplication app)
    {
        app.UseSerilogRequestLogging();
        app.UseRequestCorrelation();
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }

    /// <summary>
    /// Põe <c>request_id</c>, <c>correlation_id</c> e <c>trace_id</c> em todo log da requisição e devolve o
    /// <c>X-Request-Id</c> ao cliente. Um <c>X-Request-Id</c> recebido é mantido, para seguir o pedido entre sistemas.
    /// </summary>
    private static IApplicationBuilder UseRequestCorrelation(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var requestId = context.Request.Headers["X-Request-Id"].FirstOrDefault()
                            ?? Guid.NewGuid().ToString("N");
            var traceId = System.Diagnostics.Activity.Current?.TraceId.ToString();

            using (LogContext.PushProperty("request_id", requestId))
            using (LogContext.PushProperty("correlation_id", traceId ?? requestId))
            using (LogContext.PushProperty("trace_id", traceId ?? ""))
            {
                context.Response.Headers["X-Request-Id"] = requestId;
                await next();
            }
        });
}
