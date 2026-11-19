using Cashflow.Infrastructure.Security;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Cashflow.Infrastructure.Web.OpenApi;

/// <summary>
/// Registra o esquema OAuth2 (client credentials) com os escopos do contrato. O <c>tokenUrl</c> é
/// configuração; no local aponta para o <c>/dev/token</c> como aproximação de desenvolvimento
/// (ADR-0013). O requisito por operação é trabalho do <see cref="EscopoOperationTransformer"/>.
/// </summary>
internal sealed class OAuth2SchemeDocumentTransformer(string tokenUrl) : IOpenApiDocumentTransformer
{
    /// <summary>O nome do esquema no documento.</summary>
    public const string Nome = "OAuth2";

    public Task TransformAsync(
        OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[Nome] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Flows = new OpenApiOAuthFlows
            {
                ClientCredentials = new OpenApiOAuthFlow
                {
                    TokenUrl = new Uri(tokenUrl),
                    Scopes = new Dictionary<string, string>
                    {
                        [Scopes.LancamentosWrite] = "registrar e estornar lançamentos",
                        [Scopes.LancamentosRead] = "listar os lançamentos do dia",
                        [Scopes.ConsolidadoRead] = "consultar o consolidado diário"
                    }
                }
            }
        };
        return Task.CompletedTask;
    }
}
