using System.Security.Claims;
using System.Text.Json;
using Cashflow.Common.Auth;
using Cashflow.Common.Errors;
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

            if (body is null || body.Valor <= 0 ||
                !TipoLancamentoExtensions.TryParseTexto(body.Tipo, out var tipo))
                return CashflowProblems.LancamentoInvalido();

            if (CashflowAuth.ComercianteId(user) is not { } comercianteId)
                return CashflowProblems.TokenSemComerciante();

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
                return CashflowProblems.IdempotencyKeyObrigatoria();

            if (CashflowAuth.ComercianteId(user) is not { } comercianteId)
                return CashflowProblems.TokenSemComerciante();

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
