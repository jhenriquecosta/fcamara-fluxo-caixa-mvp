using Cashflow.Modules.Consolidado;
using Cashflow.Modules.Lancamentos;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Cashflow.Lancamentos.Api.Tests;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        await using var dataSource = new NpgsqlDataSourceBuilder(ConnectionString).Build();
        await dataSource.ApplyLancamentosSchemaAsync();
        await dataSource.ApplyConsolidadoSchemaAsync();
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}
