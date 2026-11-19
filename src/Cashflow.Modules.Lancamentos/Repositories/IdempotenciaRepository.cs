using Dapper;

namespace Cashflow.Modules.Lancamentos.Repositories;

public sealed class IdempotenciaRepository(LancamentosDbContext dbContext) : IIdempotenciaRepository
{
    private sealed record IdempotenciaRow(string CorpoHash, string Status, string? Resposta);

    public async Task<Reivindicacao> ReivindicarAsync(
        Guid comercianteId, string chave, string hash, CancellationToken cancellationToken = default)
    {
        var conn = await dbContext.GetConnectionAsync(cancellationToken);

        var reivindicado = await conn.ExecuteAsync(new CommandDefinition("""
            INSERT INTO idempotencia (comerciante_id, chave, corpo_hash, status, processando_desde)
            VALUES (@comercianteId, @chave, @hash, 'processando', now())
            ON CONFLICT (comerciante_id, chave) DO NOTHING
            """, new { comercianteId, chave, hash }, cancellationToken: cancellationToken));

        if (reivindicado == 1)
            return new Reivindicacao(ReivindicacaoTipo.Reivindicado);

        var existente = await conn.QuerySingleAsync<IdempotenciaRow>(new CommandDefinition("""
            SELECT corpo_hash AS CorpoHash, status AS Status, resposta::text AS Resposta
            FROM idempotencia
            WHERE comerciante_id = @comercianteId AND chave = @chave
            """, new { comercianteId, chave }, cancellationToken: cancellationToken));

        if (existente.CorpoHash != hash)
            return new Reivindicacao(ReivindicacaoTipo.PayloadDivergente);

        if (existente.Status is not ("processando" or "falhou"))
            return new Reivindicacao(ReivindicacaoTipo.Replay, existente.Resposta);

        var retomado = await conn.ExecuteAsync(new CommandDefinition("""
            UPDATE idempotencia
            SET status = 'processando', processando_desde = now()
            WHERE comerciante_id = @comercianteId AND chave = @chave
              AND (status = 'falhou'
                   OR (status = 'processando' AND processando_desde < now() - interval '30 seconds'))
            """, new { comercianteId, chave }, cancellationToken: cancellationToken));

        return retomado == 0
            ? new Reivindicacao(ReivindicacaoTipo.EmProcessamento)
            : new Reivindicacao(ReivindicacaoTipo.Reivindicado);
    }

    public async Task ConcluirAsync(Guid comercianteId, string chave, Guid lancamentoId, string respostaJson,
        CancellationToken cancellationToken = default)
    {
        var conn = await dbContext.GetConnectionAsync(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition("""
            UPDATE idempotencia
            SET status = 'concluido', lancamento_id = @lancamentoId, resposta = CAST(@respostaJson AS jsonb)
            WHERE comerciante_id = @comercianteId AND chave = @chave
            """, new { comercianteId, chave, lancamentoId, respostaJson },
            dbContext.CurrentTransaction, cancellationToken: cancellationToken));
    }

    public async Task FalharAsync(Guid comercianteId, string chave, CancellationToken cancellationToken = default)
    {
        var conn = await dbContext.GetConnectionAsync(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition("""
            UPDATE idempotencia SET status = 'falhou'
            WHERE comerciante_id = @comercianteId AND chave = @chave
            """, new { comercianteId, chave }, cancellationToken: cancellationToken));
    }
}
