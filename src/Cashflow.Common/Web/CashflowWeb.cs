using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Context;

namespace Cashflow.Common.Web;

public static class CashflowWeb
{
    public static WebApplication UseCashflowWeb(this WebApplication app)
    {
        app.UseSerilogRequestLogging();
        app.UseCashflowRequestContext();
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }

    private static IApplicationBuilder UseCashflowRequestContext(this IApplicationBuilder app) =>
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
