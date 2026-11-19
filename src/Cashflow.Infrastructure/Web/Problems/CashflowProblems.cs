using Microsoft.AspNetCore.Http;

namespace Cashflow.Infrastructure.Web.Problems;

/// <summary>
/// Todas as respostas de erro das APIs, em Problem Details (RFC 9457). Um lugar só para o status e a
/// mensagem de cada recusa, para que dois endpoints não respondam o mesmo erro de jeitos diferentes.
/// </summary>
public static class CashflowProblems
{
    /// <summary>400: escrita sem o cabeçalho <c>Idempotency-Key</c>, sem o qual uma nova tentativa do cliente duplicaria o lançamento.</summary>
    public static IResult IdempotencyKeyObrigatoria() =>
        Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Idempotency-Key é obrigatório");

    /// <summary>400: o corpo não é JSON válido.</summary>
    public static IResult CorpoInvalido() =>
        Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "corpo inválido");

    /// <summary>422: o JSON é válido mas viola a regra (valor não positivo ou tipo fora de <c>debito</c>/<c>credito</c>).</summary>
    public static IResult LancamentoInvalido() =>
        Results.Problem(statusCode: StatusCodes.Status422UnprocessableEntity, title: "lançamento inválido",
            detail: "valor deve ser maior que zero e tipo deve ser debito ou credito");

    /// <summary>400: a data não está no formato <c>yyyy-MM-dd</c>.</summary>
    public static IResult DataInvalida() =>
        Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "data inválida; use yyyy-MM-dd");

    /// <summary>403: o token é válido mas não diz de qual comerciante é o caixa.</summary>
    public static IResult TokenSemComerciante() =>
        Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "token sem comercianteId");

    /// <summary>422: a chave de idempotência já foi usada com outro corpo (ou, no estorno, com outro lançamento).</summary>
    public static IResult PayloadDivergente(bool estorno) =>
        Results.Problem(statusCode: StatusCodes.Status422UnprocessableEntity, title: estorno
            ? "mesma idempotency key com outro lançamento"
            : "mesma idempotency key com corpo diferente");

    /// <summary>409: outra requisição com a mesma chave ainda está sendo processada; o cliente deve tentar de novo.</summary>
    public static IResult EmProcessamento() =>
        Results.Problem(statusCode: StatusCodes.Status409Conflict,
            title: "requisição em processamento para esta idempotency key");

    /// <summary>404: o lançamento não existe ou é de outro comerciante; as duas situações respondem igual, para não vazar existência.</summary>
    public static IResult NaoEncontrado() =>
        Results.Problem(statusCode: StatusCodes.Status404NotFound,
            title: "lançamento não encontrado para este comerciante");

    /// <summary>422: pediu-se o estorno de um estorno; a correção de um estorno é um lançamento novo.</summary>
    public static IResult RegraInvalida() =>
        Results.Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
            title: "um estorno não pode ser estornado");

    /// <summary>409: o lançamento já tem estorno; um lançamento é estornado no máximo uma vez.</summary>
    public static IResult JaEstornado() =>
        Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "lançamento já estornado");

    /// <summary>500: um resultado que o endpoint não sabe traduzir. Não deveria acontecer; se acontecer, é defeito.</summary>
    public static IResult Inesperado() =>
        Results.Problem(statusCode: StatusCodes.Status500InternalServerError);
}
