using Cashflow.Infrastructure.Web.Problems;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Cashflow.Infrastructure.Data;

public static class CashflowPostgres
{
    public static IServiceCollection AddCashflowPostgres(
        this IServiceCollection services, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new CashflowConfigurationException("ConnectionStrings não configurada para o módulo");

        services.AddSingleton(_ => new NpgsqlDataSourceBuilder(connectionString).Build());
        return services;
    }
}
