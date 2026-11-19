using System.Globalization;
using System.Security.Claims;
using Cashflow.Application;
using Cashflow.Infrastructure.Security;
using Cashflow.Infrastructure.Web.Problems;
using Cashflow.Modules.Consolidado.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Cashflow.Modules.Consolidado.Endpoints;

public static class ConsolidadoEndpoints
{
    public static IEndpointRouteBuilder MapConsolidadoEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/consolidado/{data}", GetAsync).RequireAuthorization(Scopes.ConsolidadoRead);
        return app;
    }

    private static async Task<IResult> GetAsync(string data, ClaimsPrincipal user, Dispatcher dispatcher, CancellationToken ct)
    {
        if (!DateOnly.TryParseExact(data, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dia))
            return CashflowProblems.DataInvalida();

        if (CashflowAuth.ComercianteId(user) is not { } comercianteId)
            return CashflowProblems.TokenSemComerciante();

        return Results.Ok(await dispatcher.DispatchAsync(
            new GetConsolidadoDiarioQuery { ComercianteId = comercianteId, Data = dia }, ct));
    }
}
