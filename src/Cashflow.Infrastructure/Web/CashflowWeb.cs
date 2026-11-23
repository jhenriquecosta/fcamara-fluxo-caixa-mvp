using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Context;

namespace Cashflow.Infrastructure.Web;

/// <summary>O pipeline HTTP comum: correlação, log de requisição e segurança, na mesma ordem em todo host.</summary>
public static class CashflowWeb
{
    /// <summary>Contexto de correlação, log de cada requisição, autenticação e autorização.</summary>
    /// <remarks>
    /// A correlação vem antes do log de requisição: o evento final da requisição é gravado quando o
    /// <c>next()</c> volta, e nesse momento as propriedades empurradas por um middleware interno já foram
    /// descartadas — o log de acesso (inclusive o dos 401/403) sairia sem <c>request_id</c> e <c>correlation_id</c>.
    /// </remarks>
    public static WebApplication UseCashflowWeb(this WebApplication app)
    {
        app.UseRequestCorrelation();
        app.UseSerilogRequestLogging();
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
