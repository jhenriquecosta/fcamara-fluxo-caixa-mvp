using Cashflow.Domain.Repositories;
using Cashflow.Modules.Consolidado.Entities;
using Dapper;

namespace Cashflow.Modules.Consolidado.Repositories;

/// <summary>A projeção em SQL (Dapper, ADR-0006). A idempotência e a soma ficam no banco, numa transação.</summary>
public sealed class ConsolidadoRepository(ConsolidadoDbContext dbContext) : IConsolidadoRepository
{
    private sealed record ConsolidadoRow(decimal SaldoDia, decimal SaldoAcumulado);

    /// <inheritdoc />
    public IUnitOfWork UnitOfWork => dbContext;

    /// <inheritdoc />
    public async Task<ConsolidadoDiario?> ObterDoDiaAsync(Guid comercianteId, DateOnly data, CancellationToken cancellationToken = default)
    {
        var conn = await dbContext.GetConnectionAsync(cancellationToken);
        var linha = await conn.QuerySingleOrDefaultAsync<ConsolidadoRow>(new CommandDefinition("""
            SELECT saldo_dia AS SaldoDia, saldo_acumulado AS SaldoAcumulado
            FROM consolidado_diario
            WHERE comerciante_id = @comercianteId AND data = @data
            """, new { comercianteId, data }, dbContext.CurrentTransaction, cancellationToken: cancellationToken));

        return linha is null ? null : new ConsolidadoDiario(comercianteId, data, linha.SaldoDia, linha.SaldoAcumulado);
    }

    /// <inheritdoc />
    public async Task<decimal> SaldoDeAberturaAsync(Guid comercianteId, DateOnly data, CancellationToken cancellationToken = default)
    {
        var conn = await dbContext.GetConnectionAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<decimal?>(new CommandDefinition("""
            SELECT saldo_acumulado FROM consolidado_diario
            WHERE comerciante_id = @comercianteId AND data < @data
            ORDER BY data DESC LIMIT 1
            """, new { comercianteId, data }, dbContext.CurrentTransaction, cancellationToken: cancellationToken)) ?? 0m;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Três comandos na transação do chamador: o <c>INSERT ... ON CONFLICT DO NOTHING</c> em
    /// <c>evento_processado</c> é a deduplicação (inseriu zero linhas, o evento é repetido); depois, o
    /// upsert soma o movimento no dia, abrindo o dia com o acumulado do último anterior quando ele é novo;
    /// por fim, o efeito é propagado ao acumulado dos dias posteriores que já existirem.
    /// </remarks>
    public async Task<ResultadoDaProjecao> ProjetarAsync(MovimentoDoDia movimento, CancellationToken cancellationToken = default)
    {
        var conn = await dbContext.GetConnectionAsync(cancellationToken);

        var eventoNovo = await conn.ExecuteAsync(new CommandDefinition("""
            INSERT INTO evento_processado (event_id, processado_em)
            VALUES (@EventId, now())
            ON CONFLICT (event_id) DO NOTHING
            """, new { movimento.EventId }, dbContext.CurrentTransaction, cancellationToken: cancellationToken));

        if (eventoNovo == 0)
            return ResultadoDaProjecao.EventoJaProcessado;

        await conn.ExecuteAsync(new CommandDefinition("""
            INSERT INTO consolidado_diario (comerciante_id, data, saldo_dia, saldo_acumulado)
            VALUES (@ComercianteId, @Data, @Efeito,
                    COALESCE((SELECT saldo_acumulado FROM consolidado_diario
                              WHERE comerciante_id = @ComercianteId AND data < @Data
                              ORDER BY data DESC LIMIT 1), 0) + @Efeito)
            ON CONFLICT (comerciante_id, data) DO UPDATE
            SET saldo_dia = consolidado_diario.saldo_dia + EXCLUDED.saldo_dia,
                saldo_acumulado = consolidado_diario.saldo_acumulado + EXCLUDED.saldo_dia
            """, new { movimento.ComercianteId, movimento.Data, Efeito = movimento.EfeitoNoSaldo },
            dbContext.CurrentTransaction, cancellationToken: cancellationToken));

        // Chegada tardia (virada da meia-noite, reprocessamento da DLT): os dias seguintes já abertos
        // herdaram um acumulado sem este movimento. Não é retroativo — o dia é o do registro (ADR-0005).
        await conn.ExecuteAsync(new CommandDefinition("""
            UPDATE consolidado_diario
            SET saldo_acumulado = saldo_acumulado + @Efeito
            WHERE comerciante_id = @ComercianteId AND data > @Data
            """, new { movimento.ComercianteId, movimento.Data, Efeito = movimento.EfeitoNoSaldo },
            dbContext.CurrentTransaction, cancellationToken: cancellationToken));

        return ResultadoDaProjecao.Somado;
    }
}
