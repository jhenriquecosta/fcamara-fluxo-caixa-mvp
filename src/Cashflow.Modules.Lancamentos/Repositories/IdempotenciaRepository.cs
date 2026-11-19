using Dapper;

namespace Cashflow.Modules.Lancamentos.Repositories;

/// <summary>A chave de idempotência na tabela <c>idempotencia</c>, com a concorrência resolvida pelo próprio PostgreSQL.</summary>
public sealed class IdempotenciaRepository(LancamentosDbContext dbContext) : IIdempotenciaRepository
{
    private static readonly TimeSpan ReservaAbandonadaApos = TimeSpan.FromSeconds(30);

    private sealed record IdempotenciaRow(string CorpoHash, string Status, string? Resposta);

    /// <inheritdoc />
    /// <remarks>
    /// O <c>INSERT ... ON CONFLICT DO NOTHING</c> é a trava: só uma requisição consegue inserir a chave.
    /// As outras leem o que está lá e decidem entre repetir a resposta, recusar ou retomar uma reserva abandonada.
    /// </remarks>
    public async Task<ReservaDaChave> ReservarAsync(
        Guid comercianteId, string chave, string impressaoDoPedido, CancellationToken cancellationToken = default)
    {
        var conn = await dbContext.GetConnectionAsync(cancellationToken);

        var inserida = await conn.ExecuteAsync(new CommandDefinition("""
            INSERT INTO idempotencia (comerciante_id, chave, corpo_hash, status, processando_desde)
            VALUES (@comercianteId, @chave, @impressaoDoPedido, 'processando', now())
            ON CONFLICT (comerciante_id, chave) DO NOTHING
            """, new { comercianteId, chave, impressaoDoPedido }, cancellationToken: cancellationToken));

        if (inserida == 1)
            return new ReservaDaChave(SituacaoDaChave.Reservada);

        var existente = await conn.QuerySingleAsync<IdempotenciaRow>(new CommandDefinition("""
            SELECT corpo_hash AS CorpoHash, status AS Status, resposta::text AS Resposta
            FROM idempotencia
            WHERE comerciante_id = @comercianteId AND chave = @chave
            """, new { comercianteId, chave }, cancellationToken: cancellationToken));

        if (existente.CorpoHash != impressaoDoPedido)
            return new ReservaDaChave(SituacaoDaChave.UsadaComOutroPedido);

        if (existente.Status is not ("processando" or "falhou"))
            return new ReservaDaChave(SituacaoDaChave.JaConcluida, existente.Resposta);

        var retomada = await conn.ExecuteAsync(new CommandDefinition("""
            UPDATE idempotencia
            SET status = 'processando', processando_desde = now()
            WHERE comerciante_id = @comercianteId AND chave = @chave
              AND (status = 'falhou'
                   OR (status = 'processando' AND processando_desde < now() - @abandonadaApos))
            """, new { comercianteId, chave, abandonadaApos = ReservaAbandonadaApos },
            cancellationToken: cancellationToken));

        return retomada == 0
            ? new ReservaDaChave(SituacaoDaChave.EmProcessamento)
            : new ReservaDaChave(SituacaoDaChave.Reservada);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    /// <remarks>Roda fora da transação do lançamento, que já foi desfeita quando isto é chamado.</remarks>
    public async Task MarcarFalhaAsync(Guid comercianteId, string chave, CancellationToken cancellationToken = default)
    {
        var conn = await dbContext.GetConnectionAsync(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition("""
            UPDATE idempotencia SET status = 'falhou'
            WHERE comerciante_id = @comercianteId AND chave = @chave
            """, new { comercianteId, chave }, cancellationToken: cancellationToken));
    }
}
