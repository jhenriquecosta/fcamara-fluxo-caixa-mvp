using Npgsql;

namespace Cashflow.Consolidado.Api.Infrastructure;

public static class Migrator
{
    private const string ResourceName = "Cashflow.Consolidado.Api.Infrastructure.Schema.sql";

    public static async Task ApplyAsync(NpgsqlDataSource dataSource, CancellationToken ct = default)
    {
        var assembly = typeof(Migrator).Assembly;
        await using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"recurso embutido não encontrado: {ResourceName}");
        using var reader = new StreamReader(stream);
        var sql = await reader.ReadToEndAsync(ct);

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
