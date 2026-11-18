using Cashflow.Consolidado.Domain;
using Cashflow.Consolidado.Domain.Ports;
using Dapper;
using Npgsql;

namespace Cashflow.Consolidado.Infrastructure.Persistence;

public sealed class ConsolidadoRepository(NpgsqlDataSource dataSource)
    : IConsultaConsolidado, IProjecaoConsolidado
{
    private sealed record ConsolidadoRow(decimal SaldoDia, decimal SaldoAcumulado);

    public async Task<ConsolidadoDiario?> ObterAsync(
        Guid comercianteId, DateOnly data, CancellationToken ct)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);

        var linha = await conn.QuerySingleOrDefaultAsync<ConsolidadoRow>("""
            SELECT saldo_dia AS SaldoDia, saldo_acumulado AS SaldoAcumulado
            FROM consolidado_diario
            WHERE comerciante_id = @comerciante AND data = @data
            """, new { comerciante = comercianteId, data });

        return linha is null
            ? null
            : new ConsolidadoDiario(comercianteId, data, linha.SaldoDia, linha.SaldoAcumulado);
    }

    public async Task<decimal> SaldoAnteriorAsync(
        Guid comercianteId, DateOnly data, CancellationToken ct)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);

        return await conn.ExecuteScalarAsync<decimal?>("""
            SELECT saldo_acumulado FROM consolidado_diario
            WHERE comerciante_id = @comerciante AND data < @data
            ORDER BY data DESC LIMIT 1
            """, new { comerciante = comercianteId, data }) ?? 0m;
    }

    public async Task<ProjecaoResultado> AplicarAsync(LancamentoEvento evento, CancellationToken ct)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        var inserido = await conn.ExecuteAsync("""
            INSERT INTO evento_processado (event_id, processado_em)
            VALUES (@eventId, now())
            ON CONFLICT (event_id) DO NOTHING
            """, new { eventId = evento.EventId }, tx);

        if (inserido == 0)
        {
            await tx.CommitAsync(ct);
            return ProjecaoResultado.Duplicado;
        }

        await conn.ExecuteAsync("""
            INSERT INTO consolidado_diario (comerciante_id, data, saldo_dia, saldo_acumulado)
            VALUES (@comerciante, @data, @delta,
                    COALESCE((SELECT saldo_acumulado FROM consolidado_diario
                              WHERE comerciante_id = @comerciante AND data < @data
                              ORDER BY data DESC LIMIT 1), 0) + @delta)
            ON CONFLICT (comerciante_id, data) DO UPDATE
            SET saldo_dia = consolidado_diario.saldo_dia + EXCLUDED.saldo_dia,
                saldo_acumulado = consolidado_diario.saldo_acumulado + EXCLUDED.saldo_dia
            """,
            new { comerciante = evento.ComercianteId, data = evento.Data, delta = evento.Delta }, tx);

        await tx.CommitAsync(ct);
        return ProjecaoResultado.Aplicado;
    }
}
