using Cashflow.Common.Errors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Cashflow.Common.Data;

public static class CashflowPostgres
{
    public static IServiceCollection AddCashflowPostgres(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new CashflowConfigurationException("ConnectionStrings:Postgres não configurada");

        services.AddSingleton(_ => new NpgsqlDataSourceBuilder(connectionString).Build());
        return services;
    }
}
