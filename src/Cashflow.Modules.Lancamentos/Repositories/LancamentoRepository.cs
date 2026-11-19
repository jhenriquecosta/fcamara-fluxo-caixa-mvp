using Cashflow.Domain.Repositories;
using Cashflow.Modules.Lancamentos.Entities;
using Dapper;

namespace Cashflow.Modules.Lancamentos.Repositories;

/// <summary>
/// Os lançamentos na tabela <c>lancamento</c>, por SQL explícito (Dapper, ADR-0006). Toda escrita e
/// leitura usa a transação aberta do <see cref="LancamentosDbContext"/>, se houver.
/// </summary>
public sealed class LancamentoRepository(LancamentosDbContext dbContext) : ILancamentoRepository
{
    private const string Colunas = """
        id AS Id, comerciante_id AS ComercianteId, tipo AS Tipo, valor AS Valor, descricao AS Descricao,
        data AS Data, criado_em AS CriadoEm, estorno_de AS EstornoDe
        """;

    private sealed record LancamentoRow(
        Guid Id, Guid ComercianteId, string Tipo, decimal Valor, string? Descricao,
        DateOnly Data, DateTime CriadoEm, Guid? EstornoDe);

    /// <inheritdoc />
    public IUnitOfWork UnitOfWork => dbContext;

    /// <inheritdoc />
    public async Task AddAsync(Lancamento entity, CancellationToken cancellationToken = default)
    {
        var conn = await dbContext.GetConnectionAsync(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition("""
            INSERT INTO lancamento (id, comerciante_id, tipo, valor, descricao, data, criado_em, estorno_de)
            VALUES (@Id, @ComercianteId, @Tipo, @Valor, @Descricao, @Data, @CriadoEm, @EstornoDe)
            """, new
        {
            entity.Id,
            entity.ComercianteId,
            Tipo = entity.Tipo.NoContrato(),
            entity.Valor,
            entity.Descricao,
            entity.Data,
            CriadoEm = entity.CreatedDateTime,
            entity.EstornoDe
        }, dbContext.CurrentTransaction, cancellationToken: cancellationToken));
    }

    /// <inheritdoc />
    public Task<Lancamento?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        QuerySingleAsync($"SELECT {Colunas} FROM lancamento WHERE id = @id", new { id }, cancellationToken);

    /// <inheritdoc />
    public Task<Lancamento?> GetForUpdateAsync(Guid comercianteId, Guid id, CancellationToken cancellationToken = default) =>
        QuerySingleAsync($"""
            SELECT {Colunas} FROM lancamento
            WHERE id = @id AND comerciante_id = @comercianteId
            FOR UPDATE
            """, new { id, comercianteId }, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> ExisteEstornoDeAsync(Guid lancamentoId, CancellationToken cancellationToken = default)
    {
        var conn = await dbContext.GetConnectionAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT EXISTS (SELECT 1 FROM lancamento WHERE estorno_de = @lancamentoId)",
            new { lancamentoId }, dbContext.CurrentTransaction, cancellationToken: cancellationToken));
    }

    private async Task<Lancamento?> QuerySingleAsync(string sql, object param, CancellationToken cancellationToken)
    {
        var conn = await dbContext.GetConnectionAsync(cancellationToken);
        var row = await conn.QuerySingleOrDefaultAsync<LancamentoRow>(new CommandDefinition(
            sql, param, dbContext.CurrentTransaction, cancellationToken: cancellationToken));

        if (row is null)
            return null;

        if (!TipoLancamentoExtensions.TryLerDoContrato(row.Tipo, out var tipo))
            throw new InvalidOperationException($"tipo de lançamento inválido no banco: {row.Tipo}");

        return Lancamento.ReconstituirDoBanco(row.Id, row.ComercianteId, tipo, row.Valor, row.Descricao,
            row.Data, new DateTimeOffset(DateTime.SpecifyKind(row.CriadoEm, DateTimeKind.Utc)), row.EstornoDe);
    }
}
