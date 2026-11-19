using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Cashflow.Infrastructure.Web.OpenApi;

/// <summary>
/// Declara o cabeçalho <c>Idempotency-Key</c> obrigatório e o 400 de chave ausente nos endpoints
/// marcados com <see cref="IdempotencyKeyMetadata"/>.
/// </summary>
internal sealed class IdempotencyKeyOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var exigeChave = context.Description.ActionDescriptor.EndpointMetadata
            .OfType<IdempotencyKeyMetadata>()
            .Any();
        if (!exigeChave)
            return Task.CompletedTask;

        operation.Parameters ??= [];
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "Idempotency-Key",
            In = ParameterLocation.Header,
            Required = true,
            Description = "A mesma chave devolve a mesma resposta, sem duplicar o lançamento.",
            Schema = new OpenApiSchema { Type = JsonSchemaType.String }
        });

        operation.Responses ??= new OpenApiResponses();
        operation.Responses.TryAdd("400",
            ProblemDetailsOperationTransformer.Erro("Idempotency-Key ausente ou requisição inválida", context.Document));
        return Task.CompletedTask;
    }
}
