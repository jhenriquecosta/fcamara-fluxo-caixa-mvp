using System.Security.Claims;
using System.Text.Json;
using Cashflow.Common.Auth;
using Cashflow.Lancamentos.Application;
using Cashflow.Lancamentos.Domain;
using Cashflow.Lancamentos.Infrastructure.Observability;

namespace Cashflow.Lancamentos.Api.Endpoints;

public sealed record LancamentoRequest(decimal Valor, string? Tipo, string? Descricao);

public static class LancamentosEndpoints
{
    public static WebApplication MapLancamentosEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/lancamentos", async (
            HttpRequest request,
            ClaimsPrincipal user,
            RegistrarLancamentoHandler handler,
            LancamentosMetrics metrics,
            CancellationToken ct) =>
        {
            var chave = request.Headers["Idempotency-Key"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(chave))
                return Results.Problem(statusCode: 400, title: "Idempotency-Key é obrigatório");

            LancamentoRequest? body;
            try
            {
                body = await request.ReadFromJsonAsync<LancamentoRequest>(ct);
            }
            catch (JsonException)
            {
                return Results.Problem(statusCode: 400, title: "corpo inválido");
            }

            if (body is null || body.Valor <= 0 ||
                !TipoLancamentoExtensions.TryParseTexto(body.Tipo, out var tipo))
            {
                return Results.Problem(statusCode: 422, title: "lançamento inválido",
                    detail: "valor deve ser maior que zero e tipo deve ser debito ou credito");
            }

            if (CashflowAuth.ComercianteId(user) is not { } comercianteId)
                return Results.Problem(statusCode: 403, title: "token sem comercianteId");

            var resultado = await handler.HandleAsync(
                new RegistrarLancamentoCommand(comercianteId, tipo, body.Valor, body.Descricao, chave), ct);
            return Mapear(resultado, metrics);
        }).RequireAuthorization(Scopes.LancamentosWrite);

        app.MapPost("/api/v1/lancamentos/{id:guid}/estornos", async (
            Guid id,
            HttpRequest request,
            ClaimsPrincipal user,
            EstornarLancamentoHandler handler,
            LancamentosMetrics metrics,
            CancellationToken ct) =>
        {
            var chave = request.Headers["Idempotency-Key"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(chave))
                return Results.Problem(statusCode: 400, title: "Idempotency-Key é obrigatório");

            if (CashflowAuth.ComercianteId(user) is not { } comercianteId)
                return Results.Problem(statusCode: 403, title: "token sem comercianteId");

            var resultado = await handler.HandleAsync(
                new EstornarLancamentoCommand(comercianteId, id, chave), ct);
            return Mapear(resultado, metrics, estorno: true);
        }).RequireAuthorization(Scopes.LancamentosWrite);

        return app;
    }

    private static IResult Mapear(RegistroResultado resultado, LancamentosMetrics metrics, bool estorno = false)
    {
        switch (resultado)
        {
            case RegistroResultado.Criado criado:
                if (estorno) metrics.Estornado();
                else metrics.Registrado();
                return Results.Json(criado.Resposta, statusCode: 201);
            case RegistroResultado.Idempotente idempotente:
                metrics.Idempotente();
                return Results.Content(idempotente.RespostaJson, "application/json", statusCode: 201);
            case RegistroResultado.PayloadDivergente:
                metrics.ConflitoPayload();
                return Results.Problem(statusCode: 422, title: estorno
                    ? "mesma idempotency key com outro lançamento"
                    : "mesma idempotency key com corpo diferente");
            case RegistroResultado.EmProcessamento:
                metrics.ConflitoConcorrente();
                return Results.Problem(statusCode: 409,
                    title: "requisição em processamento para esta idempotency key");
            case RegistroResultado.NaoEncontrado:
                return Results.Problem(statusCode: 404, title: "lançamento não encontrado para este comerciante");
            case RegistroResultado.RegraInvalida:
                return Results.Problem(statusCode: 422, title: "um estorno não pode ser estornado");
            case RegistroResultado.Conflito:
                return Results.Problem(statusCode: 409, title: "lançamento já estornado");
            default:
                return Results.Problem(statusCode: 500);
        }
    }
}
