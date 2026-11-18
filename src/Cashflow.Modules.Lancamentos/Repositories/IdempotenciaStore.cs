using Cashflow.Modules.Lancamentos.Repositories;
using Dapper;
using Npgsql;

namespace Cashflow.Modules.Lancamentos.Repositories;

public sealed class IdempotenciaStore(NpgsqlDataSource dataSource) : IPortDeIdempotencia
{
    private sealed record IdempotenciaRow(string CorpoHash, string Status, string? Resposta);

    public async Task<Reivindicacao> ReivindicarAsync(
        Guid comercianteId, string chave, string hash, CancellationToken ct)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);

        var claimed = await conn.ExecuteAsync("""
            INSERT INTO idempotencia (comerciante_id, chave, corpo_hash, status, processando_desde)
            VALUES (@comerciante, @chave, @hash, 'processando', now())
            ON CONFLICT (comerciante_id, chave) DO NOTHING
            """, new { comerciante = comercianteId, chave, hash });

        if (claimed == 1)
            return new Reivindicacao(ReivindicacaoTipo.Reivindicado);

        var existente = await conn.QuerySingleAsync<IdempotenciaRow>("""
            SELECT corpo_hash AS CorpoHash, status AS Status, resposta::text AS Resposta
            FROM idempotencia
            WHERE comerciante_id = @comerciante AND chave = @chave
            """, new { comerciante = comercianteId, chave });

        if (existente.CorpoHash != hash)
            return new Reivindicacao(ReivindicacaoTipo.PayloadDivergente);

        if (existente.Status is "processando" or "falhou")
        {
            var reclaim = await conn.ExecuteAsync("""
                UPDATE idempotencia
                SET status = 'processando', processando_desde = now()
                WHERE comerciante_id = @comerciante AND chave = @chave
                  AND (status = 'falhou'
                       OR (status = 'processando'
                           AND processando_desde < now() - interval '30 seconds'))
                """, new { comerciante = comercianteId, chave });

            return reclaim == 0
                ? new Reivindicacao(ReivindicacaoTipo.EmProcessamento)
                : new Reivindicacao(ReivindicacaoTipo.Reivindicado);
        }

        return new Reivindicacao(ReivindicacaoTipo.Replay, existente.Resposta);
    }

    public async Task FalharAsync(Guid comercianteId, string chave, CancellationToken ct)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await conn.ExecuteAsync("""
            UPDATE idempotencia SET status = 'falhou'
            WHERE comerciante_id = @comerciante AND chave = @chave
            """, new { comerciante = comercianteId, chave });
    }
}
