using Cashflow.Domain.Repositories;
using Cashflow.Modules.Lancamentos.Entities;
using Dapper;

namespace Cashflow.Modules.Lancamentos.Repositories;

public sealed class OutboxEventRepository(LancamentosDbContext dbContext) : IOutboxEventRepository
{
    private const string Colunas = """
        id AS Id, event_type AS EventType, object_id AS ObjectId, payload::text AS Message,
        criado_em AS CriadoEm, publicado_em IS NOT NULL AS Published
        """;

    private sealed record OutboxRow(
        Guid Id, string EventType, string ObjectId, string Message, DateTime CriadoEm, bool Published);

    public IUnitOfWork UnitOfWork => dbContext;

    public async Task AddAsync(OutboxEvent entity, CancellationToken cancellationToken = default)
    {
        var conn = await dbContext.GetConnectionAsync(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition("""
            INSERT INTO outbox_event (id, event_id, event_type, object_id, payload, criado_em)
            VALUES (@Id, @Id, @EventType, @ObjectId, CAST(@Message AS jsonb), @CreatedDateTime)
            """, entity, dbContext.CurrentTransaction, cancellationToken: cancellationToken));
    }

    public async Task<OutboxEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var conn = await dbContext.GetConnectionAsync(cancellationToken);
        var row = await conn.QuerySingleOrDefaultAsync<OutboxRow>(new CommandDefinition(
            $"SELECT {Colunas} FROM outbox_event WHERE id = @id",
            new { id }, dbContext.CurrentTransaction, cancellationToken: cancellationToken));
        return row is null ? null : Mapear(row);
    }

    public async Task<IReadOnlyList<OutboxEvent>> GetPendentesAsync(int limite, CancellationToken cancellationToken = default)
    {
        var conn = await dbContext.GetConnectionAsync(cancellationToken);
        var rows = await conn.QueryAsync<OutboxRow>(new CommandDefinition($"""
            SELECT {Colunas} FROM outbox_event
            WHERE publicado_em IS NULL
            ORDER BY criado_em
            LIMIT @limite
            FOR UPDATE SKIP LOCKED
            """, new { limite }, dbContext.CurrentTransaction, cancellationToken: cancellationToken));
        return rows.Select(Mapear).ToList();
    }

    public async Task MarcarPublicadoAsync(Guid id, DateTimeOffset publicadoEm, CancellationToken cancellationToken = default)
    {
        var conn = await dbContext.GetConnectionAsync(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE outbox_event SET publicado_em = @publicadoEm WHERE id = @id",
            new { id, publicadoEm }, dbContext.CurrentTransaction, cancellationToken: cancellationToken));
    }

    public async Task<double> IdadeDoPendenteMaisAntigoAsync(CancellationToken cancellationToken = default)
    {
        var conn = await dbContext.GetConnectionAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<double>(new CommandDefinition("""
            SELECT COALESCE(EXTRACT(EPOCH FROM (now() - MIN(criado_em))), 0)::float8
            FROM outbox_event WHERE publicado_em IS NULL
            """, transaction: dbContext.CurrentTransaction, cancellationToken: cancellationToken));
    }

    private static OutboxEvent Mapear(OutboxRow row) => new()
    {
        Id = row.Id,
        EventType = row.EventType,
        ObjectId = row.ObjectId,
        Message = row.Message,
        Published = row.Published,
        CreatedDateTime = new DateTimeOffset(DateTime.SpecifyKind(row.CriadoEm, DateTimeKind.Utc))
    };
}
