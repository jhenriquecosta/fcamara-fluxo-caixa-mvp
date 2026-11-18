using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Cashflow.Infrastructure.Security;
using Cashflow.Infrastructure.Web.Problems;
using Cashflow.Modules.Consolidado.Commands;

namespace Cashflow.Modules.Consolidado.Endpoints;

public static class ConsolidadoEndpoints
{
    public static WebApplication MapConsolidadoEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/consolidado/{data}", async (
            string data,
            ClaimsPrincipal user,
            [FromServices] ConsultarConsolidadoHandler handler,
            CancellationToken ct) =>
        {
            if (!DateOnly.TryParseExact(data, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var dia))
                return CashflowProblems.DataInvalida();

            if (CashflowAuth.ComercianteId(user) is not { } comercianteId)
                return CashflowProblems.TokenSemComerciante();

            return Results.Ok(await handler.HandleAsync(comercianteId, dia, ct));
        }).RequireAuthorization(Scopes.ConsolidadoRead);

        return app;
    }
}
