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
    /// <c>/health</c> para o orquestrador (compatibilidade), <c>/health/live</c> e <c>/health/ready</c>
    /// separando processo e dependências (RF-8), <c>/metrics</c> para o Prometheus e, só em
    /// <c>Development</c>, <c>/dev/token</c> para o demo emitir um JWT local.
    /// </summary>
    public static WebApplication MapCashflowEndpoints(this WebApplication app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
        app.MapGet("/health/live", () => Results.Ok(new { status = "live" })).AllowAnonymous();
        app.MapGet("/health/ready", async (IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            var problemas = await SaudeDaApi.DependenciasForaAsync(configuration, cancellationToken);
            return problemas.Count == 0
                ? Results.Ok(new { status = "ready" })
                : Results.Json(new { status = "degraded", problemas },
                    statusCode: StatusCodes.Status503ServiceUnavailable);
        }).AllowAnonymous();

        // O emissor local só existe com a estratégia de desenvolvimento: um ambiente com Cognito
        // nunca emite token próprio, mesmo configurado por engano como Development (ADR-0012).
        if (app.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(app.Configuration["Auth:Authority"]))
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
