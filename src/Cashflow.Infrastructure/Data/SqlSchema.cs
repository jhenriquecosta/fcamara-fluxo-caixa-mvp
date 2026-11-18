using System.Reflection;
using Cashflow.Infrastructure.Web.Problems;
using Npgsql;

namespace Cashflow.Infrastructure.Data;

public static class SqlSchema
{
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
