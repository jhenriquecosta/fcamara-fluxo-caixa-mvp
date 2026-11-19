using System.Reflection;
using Cashflow.CrossCuttingConcerns.Exceptions;
using Npgsql;

namespace Cashflow.Infrastructure.Persistence;

/// <summary>Aplica o script de esquema que o módulo embute no próprio assembly (o papel do DbUp na referência).</summary>
public static class SqlSchema
{
    /// <summary>
    /// Executa o script inteiro numa conexão. Os scripts são idempotentes (<c>IF NOT EXISTS</c>), então
    /// rodar de novo num banco já migrado não muda nada.
    /// </summary>
    public static async Task ApplyAsync(
        NpgsqlDataSource dataSource, Assembly assembly, string resourceName, CancellationToken ct = default)
    {
        await using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new CashflowConfigurationException($"recurso embutido não encontrado: {resourceName}");
        using var reader = new StreamReader(stream);
        var sql = await reader.ReadToEndAsync(ct);

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
