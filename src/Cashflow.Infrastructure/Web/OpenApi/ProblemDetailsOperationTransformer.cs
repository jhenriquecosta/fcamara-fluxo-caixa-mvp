using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Cashflow.Infrastructure.Web.OpenApi;

/// <summary>
/// Declara 401 e 403 em todas as operações protegidas, no formato <c>application/problem+json</c>
/// que <c>CashflowProblems</c> devolve. Os códigos de erro específicos de cada endpoint entram por
/// <c>ProducesProblem</c>, onde fazem parte do contrato (ADR-0013).
/// </summary>
internal sealed class ProblemDetailsOperationTransformer : IOpenApiOperationTransformer
{
    /// <summary>O nome do componente único do schema de erro.</summary>
    public const string Componente = "ProblemDetails";

    public async Task TransformAsync(
        OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var anonima = context.Description.ActionDescriptor.EndpointMetadata
            .OfType<AllowAnonymousAttribute>()
            .Any();
        if (anonima)
            return;

        var problema = await context.GetOrCreateSchemaAsync(typeof(ProblemDetails), null, cancellationToken);
        context.Document?.AddComponent(Componente, problema);

        operation.Responses ??= new OpenApiResponses();
        operation.Responses.TryAdd("401", Erro("token ausente, inválido ou expirado", context.Document));
        operation.Responses.TryAdd("403", Erro("token sem o escopo exigido", context.Document));
    }

    /// <summary>A resposta de erro no formato Problem Details.</summary>
    public static OpenApiResponse Erro(string descricao, OpenApiDocument? documento) => new()
    {
        Description = descricao,
        Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/problem+json"] = new OpenApiMediaType
            {
                Schema = new OpenApiSchemaReference(Componente, documento)
            }
        }
    };
}
