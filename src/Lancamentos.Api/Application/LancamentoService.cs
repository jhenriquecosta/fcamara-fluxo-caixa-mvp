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
}

internal sealed record IdempotenciaRow(string CorpoHash, string Status, string? Resposta);

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

    public async Task<RegistroResultado> RegistrarAsync(RegistrarLancamento comando, CancellationToken ct)
    {
        var hash = HashCorpo(comando.Tipo, comando.Valor, comando.Descricao);
        var comercianteHash = HashComerciante(comando.ComercianteId);

        await using var conn = await _dataSource.OpenConnectionAsync(ct);

        var claimed = await conn.ExecuteAsync("""
            INSERT INTO idempotencia (comerciante_id, chave, corpo_hash, status, processando_desde)
            VALUES (@comerciante, @chave, @hash, 'processando', now())
            ON CONFLICT (comerciante_id, chave) DO NOTHING
            """, new { comerciante = comando.ComercianteId, chave = comando.IdempotencyKey, hash });

        if (claimed == 0)
        {
            var existente = await conn.QuerySingleAsync<IdempotenciaRow>("""
                SELECT corpo_hash AS CorpoHash, status AS Status, resposta::text AS Resposta
                FROM idempotencia
                WHERE comerciante_id = @comerciante AND chave = @chave
                """, new { comerciante = comando.ComercianteId, chave = comando.IdempotencyKey });

            if (existente.CorpoHash != hash)
            {
                _metrics.ConflitoPayload();
                _logger.LogWarning("idempotência com payload divergente {ComercianteHash} {Chave}",
                    comercianteHash, comando.IdempotencyKey);
                return new RegistroResultado.PayloadDivergente();
            }

            if (existente.Status is "processando" or "falhou")
            {
                var reclaim = await conn.ExecuteAsync("""
                    UPDATE idempotencia
                    SET status = 'processando', processando_desde = now()
                    WHERE comerciante_id = @comerciante AND chave = @chave
                      AND (status = 'falhou'
                           OR (status = 'processando'
                               AND processando_desde < now() - interval '30 seconds'))
                    """, new { comerciante = comando.ComercianteId, chave = comando.IdempotencyKey });

                if (reclaim == 0)
                {
                    _metrics.ConflitoConcorrente();
                    _logger.LogWarning("requisição em processamento para a chave {ComercianteHash} {Chave}",
                        comercianteHash, comando.IdempotencyKey);
                    return new RegistroResultado.EmProcessamento();
                }
            }
            else
            {
                _metrics.Idempotente();
                _logger.LogInformation("lançamento idempotente devolvido {ComercianteHash} {Chave}",
                    comercianteHash, comando.IdempotencyKey);
                return new RegistroResultado.Idempotente(existente.Resposta!);
            }
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
            await conn.ExecuteAsync("""
                UPDATE idempotencia SET status = 'falhou'
                WHERE comerciante_id = @comerciante AND chave = @chave
                """, new { comerciante = comando.ComercianteId, chave = comando.IdempotencyKey });
            _logger.LogError(ex, "falha ao registrar lançamento {ComercianteHash}", comercianteHash);
            throw;
        }

        _metrics.Registrado();
        _logger.LogInformation(
            "lançamento registrado {LancamentoId} {ComercianteHash} {Tipo} {Valor} {EventId}",
            id, comercianteHash, tipoTexto, comando.Valor, eventId);

        return new RegistroResultado.Criado(resposta);
    }
}
