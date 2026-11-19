using System.Net;

namespace Cashflow.Blazor.Modules.Core.Services;

/// <summary>A resposta de erro da API (ProblemDetails) traduzida para a tela.</summary>
public sealed class ApiException(HttpStatusCode statusCode, string titulo, string? detalhe = null)
    : Exception(detalhe is null ? titulo : $"{titulo}: {detalhe}")
{
    /// <summary>O status da resposta (503 quando nem houve resposta).</summary>
    public HttpStatusCode StatusCode { get; } = statusCode;

    /// <summary>O <c>title</c> do ProblemDetails.</summary>
    public string Titulo { get; } = titulo;

    /// <summary>O <c>detail</c> do ProblemDetails, quando há.</summary>
    public string? Detalhe { get; } = detalhe;

    /// <summary>A API não reconheceu o token: a tela leva ao login.</summary>
    public bool SessaoExpirada => StatusCode == HttpStatusCode.Unauthorized;

    /// <summary>O serviço não respondeu (conexão recusada ou prazo esgotado).</summary>
    public static ApiException Indisponivel() =>
        new(HttpStatusCode.ServiceUnavailable, "serviço indisponível no momento",
            "tente novamente em instantes");
}
