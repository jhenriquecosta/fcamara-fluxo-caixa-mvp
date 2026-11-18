using System.Globalization;
using System.Security.Claims;
using Cashflow.Common.Auth;
using Cashflow.Consolidado.Application;

namespace Cashflow.Consolidado.Api.Endpoints;

public static class ConsolidadoEndpoints
{
    public static WebApplication MapConsolidadoEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/consolidado/{data}", async (
            string data,
            ClaimsPrincipal user,
            ConsultarConsolidadoHandler handler,
            CancellationToken ct) =>
        {
            if (!DateOnly.TryParseExact(data, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var dia))
                return Results.Problem(statusCode: 400, title: "data inválida; use yyyy-MM-dd");

            if (CashflowAuth.ComercianteId(user) is not { } comercianteId)
                return Results.Problem(statusCode: 403, title: "token sem comercianteId");

            return Results.Ok(await handler.HandleAsync(comercianteId, dia, ct));
        }).RequireAuthorization(Scopes.ConsolidadoRead);

        return app;
    }
}
