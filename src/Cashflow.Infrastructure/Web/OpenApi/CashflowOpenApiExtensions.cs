using Cashflow.Infrastructure.Web.OpenApi;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Os transformers que fazem o documento (ADR-0013) declarar o contrato das APIs de caixa.</summary>
public static class CashflowOpenApiExtensions
{
    /// <summary>A seção de configuração do documento.</summary>
    public const string Secao = "OpenApi";

    /// <summary>O <c>tokenUrl</c> padrão do ambiente local — o <c>/dev/token</c> do ADR-0010.</summary>
    public const string TokenUrlPadrao = "http://localhost:8080/dev/token";

    /// <summary>
    /// Registra o esquema OAuth2 com os escopos, o requisito por operação lido da política, os
    /// 401/403 em ProblemDetails e a chave de idempotência dos POSTs. Chamado pelo host da API.
    /// </summary>
    /// <param name="options">As opções do gerador, vindas do <c>AddOpenApi</c>.</param>
    /// <param name="configuration">A configuração de onde sai o <c>OpenApi:TokenUrl</c>.</param>
    public static OpenApiOptions AddCashflowOpenApiTransforms(this OpenApiOptions options, IConfiguration configuration)
    {
        var tokenUrl = configuration[$"{Secao}:TokenUrl"] ?? TokenUrlPadrao;
        options.AddDocumentTransformer(new OAuth2SchemeDocumentTransformer(tokenUrl));
        options.AddOperationTransformer(new EscopoOperationTransformer());
        options.AddOperationTransformer(new ProblemDetailsOperationTransformer());
        options.AddOperationTransformer(new IdempotencyKeyOperationTransformer());
        return options;
    }
}
