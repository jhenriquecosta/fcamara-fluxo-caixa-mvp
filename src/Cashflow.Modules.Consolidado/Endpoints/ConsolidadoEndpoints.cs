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

/// <summary>A rota de leitura do módulo: valida o HTTP, monta a consulta e despacha.</summary>
public static class ConsolidadoEndpoints
{
    /// <summary>As rotas do módulo, exigindo o escopo <c>consolidado.read</c>.</summary>
    public static IEndpointRouteBuilder MapConsolidadoEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/consolidado/{data}", ConsultarDoDiaAsync)
            .RequireAuthorization(Scopes.ConsolidadoRead)
            .ProducesProblem(StatusCodes.Status400BadRequest);
        return app;
    }

    /// <summary><c>GET /api/v1/consolidado/{data}</c>: o saldo do dia <c>yyyy-MM-dd</c> do comerciante do token.</summary>
    private static async Task<IResult> ConsultarDoDiaAsync(string data, ClaimsPrincipal user, Dispatcher dispatcher, CancellationToken ct)
    {
        if (!DateOnly.TryParseExact(data, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dia))
            return CashflowProblems.DataInvalida();

        if (CashflowAuth.ComercianteId(user) is not { } comercianteId)
            return CashflowProblems.TokenSemComerciante();

        return Results.Ok(await dispatcher.DispatchAsync(
            new ConsultarConsolidadoDoDiaQuery { ComercianteId = comercianteId, Data = dia }, ct));
    }
}
