using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Cashflow.Testing;

/// <summary>Um PostgreSQL por assembly de teste, com o esquema dos dois módulos aplicado como o Migrator faria.</summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        await using var dataSource = new NpgsqlDataSourceBuilder(ConnectionString).Build();
        await dataSource.MigrateLancamentosDbAsync();
        await dataSource.MigrateConsolidadoDbAsync();
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}
