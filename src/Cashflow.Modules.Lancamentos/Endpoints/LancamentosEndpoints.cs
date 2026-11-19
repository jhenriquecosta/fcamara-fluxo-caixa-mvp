using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Cashflow.Application;
using Cashflow.Infrastructure.Security;
using Cashflow.Infrastructure.Web.OpenApi;
using Cashflow.Infrastructure.Web.Problems;
using Cashflow.Modules.Lancamentos.Commands;
using Cashflow.Modules.Lancamentos.Entities;
using Cashflow.Modules.Lancamentos.Models;
using Cashflow.Modules.Lancamentos.Observability;
using Cashflow.Modules.Lancamentos.Queries;
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
    /// <summary>As rotas do módulo, todas exigindo o escopo do token correspondente.</summary>
    public static IEndpointRouteBuilder MapLancamentosEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/lancamentos", RegistrarAsync)
            .RequireAuthorization(Scopes.LancamentosWrite)
            .WithMetadata(new IdempotencyKeyMetadata())
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
        app.MapPost("/api/v1/lancamentos/{id:guid}/estornos", EstornarAsync)
            .RequireAuthorization(Scopes.LancamentosWrite)
            .WithMetadata(new IdempotencyKeyMetadata())
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
        app.MapGet("/api/v1/lancamentos", ListarAsync)
            .RequireAuthorization(Scopes.LancamentosRead)
            .ProducesProblem(StatusCodes.Status400BadRequest);
        return app;
    }

    private static async Task<IResult> ListarAsync(
        string? data, ClaimsPrincipal user, Dispatcher dispatcher, CancellationToken ct)
    {
        DateOnly? dia = null;
        if (data is not null)
        {
            if (!DateOnly.TryParseExact(data, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                return CashflowProblems.DataInvalida();
            dia = parsed;
        }

        if (CashflowAuth.ComercianteId(user) is not { } comercianteId)
            return CashflowProblems.TokenSemComerciante();

        return Results.Ok(await dispatcher.DispatchAsync(
            new ListarLancamentosDoDiaQuery { ComercianteId = comercianteId, Data = dia }, ct));
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

        if (body is null || body.Valor <= 0 || !TipoLancamentoExtensions.TryLerDoContrato(body.Tipo, out var tipo))
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
        return ParaRespostaHttp(command.Resultado!, metrics, estorno: false);
    }

    /// <summary><c>POST /api/v1/lancamentos/{id}/estornos</c>: estorna o lançamento <paramref name="id"/> do comerciante do token.</summary>
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
        return ParaRespostaHttp(command.Resultado!, metrics, estorno: true);
    }

    /// <summary>
    /// Traduz o resultado do caso de uso em status HTTP e conta a métrica do desfecho. Uma resposta
    /// repetida devolve o JSON gravado na primeira vez, byte a byte.
    /// </summary>
    private static IResult ParaRespostaHttp(ResultadoDoLancamento resultado, LancamentosMetrics metrics, bool estorno)
    {
        switch (resultado)
        {
            case ResultadoDoLancamento.Criado criado:
                if (estorno) metrics.Estornado();
                else metrics.Registrado();
                return Results.Json(criado.Lancamento, statusCode: StatusCodes.Status201Created);
            case ResultadoDoLancamento.RespostaRepetida repetida:
                metrics.Idempotente();
                return Results.Content(repetida.RespostaGravada, "application/json", statusCode: StatusCodes.Status201Created);
            case ResultadoDoLancamento.ChaveUsadaComOutroPedido:
                metrics.ConflitoPayload();
                return CashflowProblems.PayloadDivergente(estorno);
            case ResultadoDoLancamento.ChaveEmProcessamento:
                metrics.ConflitoConcorrente();
                return CashflowProblems.EmProcessamento();
            case ResultadoDoLancamento.LancamentoNaoEncontrado:
                return CashflowProblems.NaoEncontrado();
            case ResultadoDoLancamento.EstornoDeEstorno:
                return CashflowProblems.RegraInvalida();
            case ResultadoDoLancamento.JaEstornado:
                return CashflowProblems.JaEstornado();
            default:
                return CashflowProblems.Inesperado();
        }
    }
}
