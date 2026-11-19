using Cashflow.CrossCuttingConcerns.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Cashflow.Infrastructure.Persistence;

public static class PostgresCollectionExtensions
{
    /// <summary>
    /// Um data source por módulo, registrado com a chave do módulo: num host que carrega os dois
    /// módulos (o BackgroundServer), cada um continua falando com o seu banco (ADR-0003).
    /// </summary>
    public static IServiceCollection AddModuleDataSource(
        this IServiceCollection services, string moduleKey, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new CashflowConfigurationException($"Modules:{moduleKey}:ConnectionStrings:Default não configurada");

        services.AddKeyedSingleton(moduleKey, (_, _) => new NpgsqlDataSourceBuilder(connectionString).Build());
        return services;
    }
}
