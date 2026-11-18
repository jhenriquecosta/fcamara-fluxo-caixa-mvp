using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cashflow.Lancamentos.Api.Observability;
using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Cashflow.Lancamentos.Api.Application;

public enum TipoLancamento
{
    Debito,
    Credito
}

public sealed record LancamentoRequest(decimal Valor, string? Tipo, string? Descricao);

public sealed record RegistrarLancamento(
    Guid ComercianteId,
    TipoLancamento Tipo,
    decimal Valor,
    string? Descricao,
    string IdempotencyKey);

public sealed record RegistrarEstorno(
    Guid ComercianteId,
    Guid LancamentoId,
    string IdempotencyKey);

public sealed record LancamentoResponse(
    Guid Id,
    Guid ComercianteId,
    DateOnly Data,
    decimal Valor,
    string Tipo,
    string? Descricao,
    Guid? EstornoDe);

public abstract record RegistroResultado
{
    public sealed record Criado(LancamentoResponse Resposta) : RegistroResultado;
    public sealed record Idempotente(string RespostaJson) : RegistroResultado;
    public sealed record PayloadDivergente : RegistroResultado;
    public sealed record EmProcessamento : RegistroResultado;
    public sealed record NaoEncontrado : RegistroResultado;
    public sealed record RegraInvalida : RegistroResultado;
    public sealed record Conflito : RegistroResultado;
}

internal sealed record IdempotenciaRow(string CorpoHash, string Status, string? Resposta);

internal sealed record OriginalRow(string Tipo, decimal Valor, Guid? EstornoDe);

public sealed class LancamentoService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly NpgsqlDataSource _dataSource;
    private readonly TimeProvider _clock;
    private readonly LancamentosMetrics _metrics;
    private readonly ILogger<LancamentoService> _logger;
    private readonly TimeZoneInfo _timeZone;

    public LancamentoService(
        NpgsqlDataSource dataSource,
        TimeProvider clock,
        LancamentosMetrics metrics,
        IConfiguration configuration,
        ILogger<LancamentoService> logger)
    {
        _dataSource = dataSource;
        _clock = clock;
        _metrics = metrics;
        _logger = logger;
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(
            configuration["TimeZone:Business"] ?? "America/Sao_Paulo");
    }

    public static string HashCorpo(TipoLancamento tipo, decimal valor, string? descricao)
    {
        var texto = $"{valor.ToString("0.00", CultureInfo.InvariantCulture)}|{tipo.ToString().ToLowerInvariant()}|{descricao ?? ""}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(texto))).ToLowerInvariant();
    }

    private static string HashComerciante(Guid id) =>
        Convert.ToHexString(SHA256.HashData(id.ToByteArray()))[..16].ToLowerInvariant();

    private enum ClaimResultado { Reivindicado, Replay, PayloadDivergente, EmProcessamento }

    private static async Task<(ClaimResultado resultado, string? respostaJson)> ReivindicarAsync(
        NpgsqlConnection conn, Guid comerciante, string chave, string hash)
    {
        var claimed = await conn.ExecuteAsync("""
            INSERT INTO idempotencia (comerciante_id, chave, corpo_hash, status, processando_desde)
            VALUES (@comerciante, @chave, @hash, 'processando', now())
            ON CONFLICT (comerciante_id, chave) DO NOTHING
            """, new { comerciante, chave, hash });

        if (claimed == 1)
            return (ClaimResultado.Reivindicado, null);

        var existente = await conn.QuerySingleAsync<IdempotenciaRow>("""
            SELECT corpo_hash AS CorpoHash, status AS Status, resposta::text AS Resposta
            FROM idempotencia
            WHERE comerciante_id = @comerciante AND chave = @chave
            """, new { comerciante, chave });

        if (existente.CorpoHash != hash)
            return (ClaimResultado.PayloadDivergente, null);

        if (existente.Status is "processando" or "falhou")
        {
            var reclaim = await conn.ExecuteAsync("""
                UPDATE idempotencia
                SET status = 'processando', processando_desde = now()
                WHERE comerciante_id = @comerciante AND chave = @chave
                  AND (status = 'falhou'
                       OR (status = 'processando'
                           AND processando_desde < now() - interval '30 seconds'))
                """, new { comerciante, chave });

            return reclaim == 0
                ? (ClaimResultado.EmProcessamento, null)
                : (ClaimResultado.Reivindicado, null);
        }

        return (ClaimResultado.Replay, existente.Resposta);
    }

    private static Task MarcarFalhouAsync(NpgsqlConnection conn, Guid comerciante, string chave) =>
        conn.ExecuteAsync("""
            UPDATE idempotencia SET status = 'falhou'
            WHERE comerciante_id = @comerciante AND chave = @chave
            """, new { comerciante, chave });

    public async Task<RegistroResultado> RegistrarAsync(RegistrarLancamento comando, CancellationToken ct)
    {
        var hash = HashCorpo(comando.Tipo, comando.Valor, comando.Descricao);
        var comercianteHash = HashComerciante(comando.ComercianteId);

        await using var conn = await _dataSource.OpenConnectionAsync(ct);

        var (claim, replayJson) = await ReivindicarAsync(
            conn, comando.ComercianteId, comando.IdempotencyKey, hash);

        switch (claim)
        {
            case ClaimResultado.PayloadDivergente:
                _metrics.ConflitoPayload();
                _logger.LogWarning("idempotência com payload divergente {ComercianteHash} {Chave}",
                    comercianteHash, comando.IdempotencyKey);
                return new RegistroResultado.PayloadDivergente();
            case ClaimResultado.EmProcessamento:
                _metrics.ConflitoConcorrente();
                _logger.LogWarning("requisição em processamento para a chave {ComercianteHash} {Chave}",
                    comercianteHash, comando.IdempotencyKey);
                return new RegistroResultado.EmProcessamento();
            case ClaimResultado.Replay:
                _metrics.Idempotente();
                _logger.LogInformation("lançamento idempotente devolvido {ComercianteHash} {Chave}",
                    comercianteHash, comando.IdempotencyKey);
                return new RegistroResultado.Idempotente(replayJson!);
        }

        var agora = _clock.GetUtcNow();
        var data = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(agora.UtcDateTime, _timeZone));
        var id = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var tipoTexto = comando.Tipo.ToString().ToLowerInvariant();
        var resposta = new LancamentoResponse(id, comando.ComercianteId, data, comando.Valor,
            tipoTexto, comando.Descricao, null);
        var respostaJson = JsonSerializer.Serialize(resposta, JsonOptions);
        var envelope = JsonSerializer.Serialize(new
        {
            eventId,
            eventType = "lancamento.registrado",
            occurredAt = agora,
            version = 1,
            payload = new
            {
                lancamentoId = id,
                comercianteId = comando.ComercianteId,
                tipo = tipoTexto,
                valor = comando.Valor,
                data,
                estornoDe = (Guid?)null
            }
        }, JsonOptions);

        try
        {
            await using var tx = await conn.BeginTransactionAsync(ct);

            await conn.ExecuteAsync("""
                INSERT INTO lancamento (id, comerciante_id, tipo, valor, descricao, data, criado_em)
                VALUES (@id, @comerciante, @tipo, @valor, @descricao, @data, @criadoEm)
                """, new
            {
                id,
                comerciante = comando.ComercianteId,
                tipo = tipoTexto,
                valor = comando.Valor,
                descricao = comando.Descricao,
                data,
                criadoEm = agora
            }, tx);

            await conn.ExecuteAsync("""
                INSERT INTO outbox_event (id, event_id, payload, criado_em)
                VALUES (@id, @eventId, CAST(@payload AS jsonb), @criadoEm)
                """, new
            {
                id = Guid.NewGuid(),
                eventId,
                payload = envelope,
                criadoEm = agora
            }, tx);

            await conn.ExecuteAsync("""
                UPDATE idempotencia
                SET status = 'concluido', lancamento_id = @id, resposta = CAST(@resposta AS jsonb)
                WHERE comerciante_id = @comerciante AND chave = @chave
                """, new
            {
                id,
                comerciante = comando.ComercianteId,
                chave = comando.IdempotencyKey,
                resposta = respostaJson
            }, tx);

            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await MarcarFalhouAsync(conn, comando.ComercianteId, comando.IdempotencyKey);
            _logger.LogError(ex, "falha ao registrar lançamento {ComercianteHash}", comercianteHash);
            throw;
        }

        _metrics.Registrado();
        _logger.LogInformation(
            "lançamento registrado {LancamentoId} {ComercianteHash} {Tipo} {Valor} {EventId}",
            id, comercianteHash, tipoTexto, comando.Valor, eventId);

        return new RegistroResultado.Criado(resposta);
    }

    public async Task<RegistroResultado> EstornarAsync(RegistrarEstorno comando, CancellationToken ct)
    {
        var hash = comando.LancamentoId.ToString();
        var comercianteHash = HashComerciante(comando.ComercianteId);

        await using var conn = await _dataSource.OpenConnectionAsync(ct);

        var (claim, replayJson) = await ReivindicarAsync(
            conn, comando.ComercianteId, comando.IdempotencyKey, hash);

        switch (claim)
        {
            case ClaimResultado.PayloadDivergente:
                _metrics.ConflitoPayload();
                return new RegistroResultado.PayloadDivergente();
            case ClaimResultado.EmProcessamento:
                _metrics.ConflitoConcorrente();
                return new RegistroResultado.EmProcessamento();
            case ClaimResultado.Replay:
                _metrics.Idempotente();
                return new RegistroResultado.Idempotente(replayJson!);
        }

        try
        {
            await using var tx = await conn.BeginTransactionAsync(ct);

            var original = await conn.QuerySingleOrDefaultAsync<OriginalRow>("""
                SELECT tipo AS Tipo, valor AS Valor, estorno_de AS EstornoDe
                FROM lancamento
                WHERE id = @id AND comerciante_id = @comerciante
                FOR UPDATE
                """, new { id = comando.LancamentoId, comerciante = comando.ComercianteId }, tx);

            if (original is null)
            {
                await tx.RollbackAsync(ct);
                await MarcarFalhouAsync(conn, comando.ComercianteId, comando.IdempotencyKey);
                return new RegistroResultado.NaoEncontrado();
            }

            if (original.EstornoDe is not null)
            {
                await tx.RollbackAsync(ct);
                await MarcarFalhouAsync(conn, comando.ComercianteId, comando.IdempotencyKey);
                return new RegistroResultado.RegraInvalida();
            }

            var jaEstornado = await conn.ExecuteScalarAsync<int>(
                "SELECT count(*) FROM lancamento WHERE estorno_de = @id",
                new { id = comando.LancamentoId }, tx);

            if (jaEstornado > 0)
            {
                await tx.RollbackAsync(ct);
                await MarcarFalhouAsync(conn, comando.ComercianteId, comando.IdempotencyKey);
                return new RegistroResultado.Conflito();
            }

            var agora = _clock.GetUtcNow();
            var data = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(agora.UtcDateTime, _timeZone));
            var id = Guid.NewGuid();
            var eventId = Guid.NewGuid();
            var tipoOposto = original.Tipo == "credito" ? "debito" : "credito";
            var resposta = new LancamentoResponse(id, comando.ComercianteId, data,
                original.Valor, tipoOposto, null, comando.LancamentoId);
            var respostaJson = JsonSerializer.Serialize(resposta, JsonOptions);
            var envelope = JsonSerializer.Serialize(new
            {
                eventId,
                eventType = "lancamento.estornado",
                occurredAt = agora,
                version = 1,
                payload = new
                {
                    lancamentoId = id,
                    comercianteId = comando.ComercianteId,
                    tipo = tipoOposto,
                    valor = original.Valor,
                    data,
                    estornoDe = comando.LancamentoId
                }
            }, JsonOptions);

            await conn.ExecuteAsync("""
                INSERT INTO lancamento (id, comerciante_id, tipo, valor, descricao, data, criado_em, estorno_de)
                VALUES (@id, @comerciante, @tipo, @valor, NULL, @data, @criadoEm, @estornoDe)
                """, new
            {
                id,
                comerciante = comando.ComercianteId,
                tipo = tipoOposto,
                valor = original.Valor,
                data,
                criadoEm = agora,
                estornoDe = comando.LancamentoId
            }, tx);

            await conn.ExecuteAsync("""
                INSERT INTO outbox_event (id, event_id, payload, criado_em)
                VALUES (@id, @eventId, CAST(@payload AS jsonb), @criadoEm)
                """, new
            {
                id = Guid.NewGuid(),
                eventId,
                payload = envelope,
                criadoEm = agora
            }, tx);

            await conn.ExecuteAsync("""
                UPDATE idempotencia
                SET status = 'concluido', lancamento_id = @id, resposta = CAST(@resposta AS jsonb)
                WHERE comerciante_id = @comerciante AND chave = @chave
                """, new
            {
                id,
                comerciante = comando.ComercianteId,
                chave = comando.IdempotencyKey,
                resposta = respostaJson
            }, tx);

            await tx.CommitAsync(ct);
            _metrics.Estornado();
            _logger.LogInformation(
                "estorno registrado {LancamentoId} {EstornoDe} {ComercianteHash} {EventId}",
                id, comando.LancamentoId, comercianteHash, eventId);

            return new RegistroResultado.Criado(resposta);
        }
        catch (Exception ex)
        {
            await MarcarFalhouAsync(conn, comando.ComercianteId, comando.IdempotencyKey);
            _logger.LogError(ex, "falha ao estornar lançamento {EstornoDe} {ComercianteHash}",
                comando.LancamentoId, comercianteHash);
            throw;
        }
    }
}
