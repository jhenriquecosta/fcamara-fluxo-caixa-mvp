using Cashflow.Infrastructure.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;

namespace Cashflow.Infrastructure.Web;

/// <summary>Os endpoints operacionais que todo host expõe, independentemente dos módulos ligados.</summary>
public static class CashflowEndpoints
{
    /// <summary>
    /// <c>/health</c> para o orquestrador, <c>/metrics</c> para o Prometheus e, só em
    /// <c>Development</c>, <c>/dev/token</c> para o demo emitir um JWT local.
    /// </summary>
    public static WebApplication MapCashflowEndpoints(this WebApplication app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

        if (app.Environment.IsDevelopment())
        {
            app.MapPost("/dev/token", (DevTokenRequest request, IConfiguration configuration) =>
                    Results.Ok(new
                    {
                        token = DevToken.Create(configuration, request.ComercianteId,
                            request.Scopes ?? Scopes.Todos)
                    }))
                .AllowAnonymous();
        }

        app.MapPrometheusScrapingEndpoint();
        return app;
    }
}
