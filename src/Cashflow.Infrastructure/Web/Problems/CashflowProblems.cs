using Microsoft.AspNetCore.Http;

namespace Cashflow.Infrastructure.Web.Problems;

public static class CashflowProblems
{
    public static IResult IdempotencyKeyObrigatoria() =>
        Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Idempotency-Key é obrigatório");

    public static IResult CorpoInvalido() =>
        Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "corpo inválido");

    public static IResult LancamentoInvalido() =>
        Results.Problem(statusCode: StatusCodes.Status422UnprocessableEntity, title: "lançamento inválido",
            detail: "valor deve ser maior que zero e tipo deve ser debito ou credito");

    public static IResult DataInvalida() =>
        Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "data inválida; use yyyy-MM-dd");

    public static IResult TokenSemComerciante() =>
        Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "token sem comercianteId");

    public static IResult PayloadDivergente(bool estorno) =>
        Results.Problem(statusCode: StatusCodes.Status422UnprocessableEntity, title: estorno
            ? "mesma idempotency key com outro lançamento"
            : "mesma idempotency key com corpo diferente");

    public static IResult EmProcessamento() =>
        Results.Problem(statusCode: StatusCodes.Status409Conflict,
            title: "requisição em processamento para esta idempotency key");

    public static IResult NaoEncontrado() =>
        Results.Problem(statusCode: StatusCodes.Status404NotFound,
            title: "lançamento não encontrado para este comerciante");

    public static IResult RegraInvalida() =>
        Results.Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
            title: "um estorno não pode ser estornado");

    public static IResult JaEstornado() =>
        Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "lançamento já estornado");

    public static IResult Inesperado() =>
        Results.Problem(statusCode: StatusCodes.Status500InternalServerError);
}
