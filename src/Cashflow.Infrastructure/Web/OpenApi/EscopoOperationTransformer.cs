using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Cashflow.Infrastructure.Web.OpenApi;

/// <summary>
/// Declara, por operação, o escopo que a política de autorização já exige: a política é o escopo.
/// Endpoint sem política (anonimo ou sem autorização declarada) sai sem requisito.
/// </summary>
internal sealed class EscopoOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var escopos = context.Description.ActionDescriptor.EndpointMetadata
            .OfType<IAuthorizeData>()
            .Select(autorizacao => autorizacao.Policy)
            .Where(politica => !string.IsNullOrEmpty(politica))
            .Distinct()
            .ToList();

        if (escopos.Count == 0)
            return Task.CompletedTask;

        operation.Security ??= [];
        foreach (var escopo in escopos)
        {
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(OAuth2SchemeDocumentTransformer.Nome, context.Document)] = [escopo!]
            });
        }
        return Task.CompletedTask;
    }
}
