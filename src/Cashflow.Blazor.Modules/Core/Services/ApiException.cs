using System.Net;

namespace Cashflow.Blazor.Modules.Core.Services;

/// <summary>A resposta de erro da API (ProblemDetails) traduzida para a tela.</summary>
public sealed class ApiException(HttpStatusCode statusCode, string titulo, string? detalhe = null)
    : Exception(detalhe is null ? titulo : $"{titulo}: {detalhe}")
{
    public HttpStatusCode StatusCode { get; } = statusCode;

    public string Titulo { get; } = titulo;

    public string? Detalhe { get; } = detalhe;

    public bool SessaoExpirada => StatusCode == HttpStatusCode.Unauthorized;

    public static ApiException Indisponivel() =>
        new(HttpStatusCode.ServiceUnavailable, "serviço indisponível no momento",
            "tente novamente em instantes");
}
