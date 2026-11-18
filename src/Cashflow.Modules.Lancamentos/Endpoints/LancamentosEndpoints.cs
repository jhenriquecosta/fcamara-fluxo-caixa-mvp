using System.Security.Claims;
using System.Text.Json;
using Cashflow.Application;
using Cashflow.Infrastructure.Security;
using Cashflow.Infrastructure.Web.Problems;
using Cashflow.Modules.Lancamentos.Commands;
using Cashflow.Modules.Lancamentos.Entities;
using Cashflow.Modules.Lancamentos.Models;
using Cashflow.Modules.Lancamentos.Observability;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Cashflow.Modules.Lancamentos.Endpoints;

/// <summary>
/// O papel do <c>ProductsController</c> da referência em minimal API: valida o HTTP, monta o comando
/// e despacha. A regra de negócio está no handler; o endpoint só traduz o resultado em status.
/// </summary>
public static class LancamentosEndpoints
{
    public static IEndpointRouteBuilder MapLancamentosEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/lancamentos", RegistrarAsync).RequireAuthorization(Scopes.LancamentosWrite);
        app.MapPost("/api/v1/lancamentos/{id:guid}/estornos", EstornarAsync).RequireAuthorization(Scopes.LancamentosWrite);
        return app;
    }

    private static async Task<IResult> RegistrarAsync(
        HttpRequest request, ClaimsPrincipal user, Dispatcher dispatcher, LancamentosMetrics metrics, CancellationToken ct)
    {
        var chave = request.Headers["Idempotency-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(chave))
            return CashflowProblems.IdempotencyKeyObrigatoria();

        LancamentoRequest? body;
        try
        {
            body = await request.ReadFromJsonAsync<LancamentoRequest>(ct);
        }
        catch (JsonException)
        {
            return CashflowProblems.CorpoInvalido();
        }

        if (body is null || body.Valor <= 0 || !TipoLancamentoExtensions.TryParseTexto(body.Tipo, out var tipo))
            return CashflowProblems.LancamentoInvalido();

        if (CashflowAuth.ComercianteId(user) is not { } comercianteId)
            return CashflowProblems.TokenSemComerciante();

        var command = new RegistrarLancamentoCommand
        {
            ComercianteId = comercianteId,
            Tipo = tipo,
            Valor = body.Valor,
            Descricao = body.Descricao,
            IdempotencyKey = chave
        };
        await dispatcher.DispatchAsync(command, ct);
        return Mapear(command.Resultado!, metrics, estorno: false);
    }

    private static async Task<IResult> EstornarAsync(
        Guid id, HttpRequest request, ClaimsPrincipal user, Dispatcher dispatcher, LancamentosMetrics metrics,
        CancellationToken ct)
    {
        var chave = request.Headers["Idempotency-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(chave))
            return CashflowProblems.IdempotencyKeyObrigatoria();

        if (CashflowAuth.ComercianteId(user) is not { } comercianteId)
            return CashflowProblems.TokenSemComerciante();

        var command = new EstornarLancamentoCommand
        {
            ComercianteId = comercianteId,
            LancamentoId = id,
            IdempotencyKey = chave
        };
        await dispatcher.DispatchAsync(command, ct);
        return Mapear(command.Resultado!, metrics, estorno: true);
    }

    private static IResult Mapear(RegistroResultado resultado, LancamentosMetrics metrics, bool estorno)
    {
        switch (resultado)
        {
            case RegistroResultado.Criado criado:
                if (estorno) metrics.Estornado();
                else metrics.Registrado();
                return Results.Json(criado.Lancamento, statusCode: StatusCodes.Status201Created);
            case RegistroResultado.Idempotente idempotente:
                metrics.Idempotente();
                return Results.Content(idempotente.RespostaJson, "application/json", statusCode: StatusCodes.Status201Created);
            case RegistroResultado.PayloadDivergente:
                metrics.ConflitoPayload();
                return CashflowProblems.PayloadDivergente(estorno);
            case RegistroResultado.EmProcessamento:
                metrics.ConflitoConcorrente();
                return CashflowProblems.EmProcessamento();
            case RegistroResultado.NaoEncontrado:
                return CashflowProblems.NaoEncontrado();
            case RegistroResultado.RegraInvalida:
                return CashflowProblems.RegraInvalida();
            case RegistroResultado.Conflito:
                return CashflowProblems.JaEstornado();
            default:
                return CashflowProblems.Inesperado();
        }
    }
}
