using System.Text.Json;
using Cashflow.Contracts;
using Cashflow.Lancamentos.Application;
using Cashflow.Lancamentos.Domain;
using Cashflow.Lancamentos.Domain.Ports;
using Dapper;
using Npgsql;

namespace Cashflow.Lancamentos.Infrastructure.Persistence;

public sealed class LancamentoRepository(NpgsqlDataSource dataSource) : IRegistroDeLancamentos
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed record OriginalRow(
        string Tipo, decimal Valor, string? Descricao, DateOnly Data, DateTime CriadoEm, Guid? EstornoDe);

    public async Task RegistrarAsync(
        Lancamento lancamento, string respostaJson, string chaveIdempotencia, CancellationToken ct)
    {
        var eventId = Guid.NewGuid();
        var evento = JsonSerializer.Serialize(new CashflowEvent<LancamentoEventPayload>(
            eventId, EventTypes.LancamentoRegistrado, lancamento.CriadoEm, 1,
            new LancamentoEventPayload(
                lancamento.Id, lancamento.ComercianteId, lancamento.Tipo.Texto(),
                lancamento.Valor, lancamento.Data, null)), JsonOptions);

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        await InserirLancamentoAsync(conn, tx, lancamento, ct);
        await InserirEventoAsync(conn, tx, eventId, evento, lancamento.CriadoEm, ct);
        await ConcluirIdempotenciaAsync(conn, tx, lancamento, respostaJson, chaveIdempotencia, ct);

        await tx.CommitAsync(ct);
    }

    public async Task<(EstornoResultado resultado, Lancamento? estorno)> EstornarAsync(
        Guid comercianteId, Guid lancamentoId, DateOnly data, DateTimeOffset agora,
        string chaveIdempotencia, CancellationToken ct)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        var original = await conn.QuerySingleOrDefaultAsync<OriginalRow>("""
            SELECT tipo AS Tipo, valor AS Valor, descricao AS Descricao, data AS Data,
                   criado_em AS CriadoEm, estorno_de AS EstornoDe
            FROM lancamento
            WHERE id = @id AND comerciante_id = @comerciante
            FOR UPDATE
            """, new { id = lancamentoId, comerciante = comercianteId }, tx);

        if (original is null)
        {
            await tx.RollbackAsync(ct);
            return (EstornoResultado.NaoEncontrado, null);
        }

        if (original.EstornoDe is not null)
        {
            await tx.RollbackAsync(ct);
            return (EstornoResultado.RegraInvalida, null);
        }

        var jaEstornado = await conn.ExecuteScalarAsync<int>(
            "SELECT count(*) FROM lancamento WHERE estorno_de = @id",
            new { id = lancamentoId }, tx);

        if (jaEstornado > 0)
        {
            await tx.RollbackAsync(ct);
            return (EstornoResultado.Conflito, null);
        }

        if (!TipoLancamentoExtensions.TryParseTexto(original.Tipo, out var tipo))
        {
            await tx.RollbackAsync(ct);
            return (EstornoResultado.RegraInvalida, null);
        }

        var originalLancamento = new Lancamento(
            lancamentoId, comercianteId, tipo, original.Valor, original.Descricao,
            original.Data, new DateTimeOffset(original.CriadoEm), original.EstornoDe);

        var estorno = Lancamento.Estornar(originalLancamento, data, agora);

        var resposta = new LancamentoResponse(
            estorno.Id, estorno.ComercianteId, estorno.Data, estorno.Valor,
            estorno.Tipo.Texto(), null, estorno.EstornoDe);
        var respostaJson = JsonSerializer.Serialize(resposta, JsonOptions);

        var eventId = Guid.NewGuid();
        var evento = JsonSerializer.Serialize(new CashflowEvent<LancamentoEventPayload>(
            eventId, EventTypes.LancamentoEstornado, agora, 1,
            new LancamentoEventPayload(
                estorno.Id, estorno.ComercianteId, estorno.Tipo.Texto(),
                estorno.Valor, estorno.Data, estorno.EstornoDe)), JsonOptions);

        await InserirLancamentoAsync(conn, tx, estorno, ct);
        await InserirEventoAsync(conn, tx, eventId, evento, agora, ct);
        await ConcluirIdempotenciaAsync(conn, tx, estorno, respostaJson, chaveIdempotencia, ct);

        await tx.CommitAsync(ct);
        return (EstornoResultado.Criado, estorno);
    }

    private static Task InserirLancamentoAsync(
        NpgsqlConnection conn, NpgsqlTransaction tx, Lancamento lancamento, CancellationToken ct) =>
        conn.ExecuteAsync("""
            INSERT INTO lancamento (id, comerciante_id, tipo, valor, descricao, data, criado_em, estorno_de)
            VALUES (@id, @comerciante, @tipo, @valor, @descricao, @data, @criadoEm, @estornoDe)
            """, new
        {
            id = lancamento.Id,
            comerciante = lancamento.ComercianteId,
            tipo = lancamento.Tipo.Texto(),
            valor = lancamento.Valor,
            descricao = lancamento.Descricao,
            data = lancamento.Data,
            criadoEm = lancamento.CriadoEm,
            estornoDe = lancamento.EstornoDe
        }, tx);

    private static Task InserirEventoAsync(
        NpgsqlConnection conn, NpgsqlTransaction tx, Guid eventId, string payload,
        DateTimeOffset criadoEm, CancellationToken ct) =>
        conn.ExecuteAsync("""
            INSERT INTO outbox_event (id, event_id, payload, criado_em)
            VALUES (@id, @eventId, CAST(@payload AS jsonb), @criadoEm)
            """, new
        {
            id = Guid.NewGuid(),
            eventId,
            payload,
            criadoEm
        }, tx);

    private static Task ConcluirIdempotenciaAsync(
        NpgsqlConnection conn, NpgsqlTransaction tx, Lancamento lancamento, string respostaJson,
        string chaveIdempotencia, CancellationToken ct) =>
        conn.ExecuteAsync("""
            UPDATE idempotencia
            SET status = 'concluido', lancamento_id = @id, resposta = CAST(@resposta AS jsonb)
            WHERE comerciante_id = @comerciante AND chave = @chave
            """, new
        {
            id = lancamento.Id,
            comerciante = lancamento.ComercianteId,
            chave = chaveIdempotencia,
            resposta = respostaJson
        }, tx);
}
