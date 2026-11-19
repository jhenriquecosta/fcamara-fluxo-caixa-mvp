using Cashflow.CrossCuttingConcerns.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Cashflow.Infrastructure.Persistence;

/// <summary>O registro do PostgreSQL de cada módulo.</summary>
public static class PostgresCollectionExtensions
{
    /// <summary>
    /// Um data source por módulo, registrado com a chave do módulo. Num host que carrega os dois módulos
    /// (o BackgroundServer), cada um continua falando com o seu banco (ADR-0003).
    /// </summary>
    /// <exception cref="CashflowConfigurationException">A connection string do módulo não foi configurada.</exception>
    public static IServiceCollection AddModuleDataSource(
        this IServiceCollection services, string moduleKey, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new CashflowConfigurationException($"Modules:{moduleKey}:ConnectionStrings:Default não configurada");

        services.AddKeyedSingleton(moduleKey, (_, _) => new NpgsqlDataSourceBuilder(connectionString).Build());
        return services;
    }
}
