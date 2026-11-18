using Dapper;
using Npgsql;

namespace Cashflow.Consolidado.Api.Application;

public sealed record ConsolidadoResponse(
    Guid ComercianteId,
    DateOnly Data,
    decimal SaldoDia,
    decimal SaldoAnterior,
    decimal SaldoAcumulado);

internal sealed record ConsolidadoRow(decimal SaldoDia, decimal SaldoAcumulado);

public sealed class ConsolidadoService(NpgsqlDataSource dataSource)
{
    public async Task<ConsolidadoResponse> ObterAsync(Guid comercianteId, DateOnly data, CancellationToken ct)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);

        var linha = await conn.QuerySingleOrDefaultAsync<ConsolidadoRow>("""
            SELECT saldo_dia AS SaldoDia, saldo_acumulado AS SaldoAcumulado
            FROM consolidado_diario
            WHERE comerciante_id = @comerciante AND data = @data
            """, new { comerciante = comercianteId, data });

        if (linha is null)
        {
            var anterior = await conn.ExecuteScalarAsync<decimal?>("""
                SELECT saldo_acumulado FROM consolidado_diario
                WHERE comerciante_id = @comerciante AND data < @data
                ORDER BY data DESC LIMIT 1
                """, new { comerciante = comercianteId, data }) ?? 0m;
            return new ConsolidadoResponse(comercianteId, data, 0m, anterior, anterior);
        }

        return new ConsolidadoResponse(comercianteId, data, linha.SaldoDia,
            linha.SaldoAcumulado - linha.SaldoDia, linha.SaldoAcumulado);
    }
}
