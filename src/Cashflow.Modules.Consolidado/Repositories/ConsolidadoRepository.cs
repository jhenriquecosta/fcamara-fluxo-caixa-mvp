using Cashflow.Domain.Repositories;
using Cashflow.Modules.Consolidado.Entities;
using Dapper;

namespace Cashflow.Modules.Consolidado.Repositories;

public sealed class ConsolidadoRepository(ConsolidadoDbContext dbContext) : IConsolidadoRepository
{
    private sealed record ConsolidadoRow(decimal SaldoDia, decimal SaldoAcumulado);

    public IUnitOfWork UnitOfWork => dbContext;

    public async Task<ConsolidadoDiario?> GetAsync(Guid comercianteId, DateOnly data, CancellationToken cancellationToken = default)
    {
        var conn = await dbContext.GetConnectionAsync(cancellationToken);
        var linha = await conn.QuerySingleOrDefaultAsync<ConsolidadoRow>(new CommandDefinition("""
            SELECT saldo_dia AS SaldoDia, saldo_acumulado AS SaldoAcumulado
            FROM consolidado_diario
            WHERE comerciante_id = @comercianteId AND data = @data
            """, new { comercianteId, data }, dbContext.CurrentTransaction, cancellationToken: cancellationToken));

        return linha is null ? null : new ConsolidadoDiario(comercianteId, data, linha.SaldoDia, linha.SaldoAcumulado);
    }

    public async Task<decimal> SaldoAnteriorAsync(Guid comercianteId, DateOnly data, CancellationToken cancellationToken = default)
    {
        var conn = await dbContext.GetConnectionAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<decimal?>(new CommandDefinition("""
            SELECT saldo_acumulado FROM consolidado_diario
            WHERE comerciante_id = @comercianteId AND data < @data
            ORDER BY data DESC LIMIT 1
            """, new { comercianteId, data }, dbContext.CurrentTransaction, cancellationToken: cancellationToken)) ?? 0m;
    }

    public async Task<ProjecaoResultado> AplicarAsync(MovimentoDoDia movimento, CancellationToken cancellationToken = default)
    {
        var conn = await dbContext.GetConnectionAsync(cancellationToken);

        var inserido = await conn.ExecuteAsync(new CommandDefinition("""
            INSERT INTO evento_processado (event_id, processado_em)
            VALUES (@EventId, now())
            ON CONFLICT (event_id) DO NOTHING
            """, new { movimento.EventId }, dbContext.CurrentTransaction, cancellationToken: cancellationToken));

        if (inserido == 0)
            return ProjecaoResultado.Duplicado;

        await conn.ExecuteAsync(new CommandDefinition("""
            INSERT INTO consolidado_diario (comerciante_id, data, saldo_dia, saldo_acumulado)
            VALUES (@ComercianteId, @Data, @Delta,
                    COALESCE((SELECT saldo_acumulado FROM consolidado_diario
                              WHERE comerciante_id = @ComercianteId AND data < @Data
                              ORDER BY data DESC LIMIT 1), 0) + @Delta)
            ON CONFLICT (comerciante_id, data) DO UPDATE
            SET saldo_dia = consolidado_diario.saldo_dia + EXCLUDED.saldo_dia,
                saldo_acumulado = consolidado_diario.saldo_acumulado + EXCLUDED.saldo_dia
            """, new { movimento.ComercianteId, movimento.Data, movimento.Delta },
            dbContext.CurrentTransaction, cancellationToken: cancellationToken));

        return ProjecaoResultado.Aplicado;
    }
}
