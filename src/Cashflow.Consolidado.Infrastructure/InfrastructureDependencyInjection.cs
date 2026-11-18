using Cashflow.Consolidado.Domain.Ports;
using Cashflow.Consolidado.Infrastructure.Messaging;
using Cashflow.Consolidado.Infrastructure.Observability;
using Cashflow.Consolidado.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Cashflow.Consolidado.Infrastructure;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddConsolidadoInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(_ => new NpgsqlDataSourceBuilder(
                configuration.GetConnectionString("Postgres")
                ?? throw new InvalidOperationException("ConnectionStrings:Postgres não configurada")).Build());

        services.AddSingleton<ConsolidadoMetrics>();
        services.AddSingleton<ConsolidadoRepository>();
        services.AddSingleton<IConsultaConsolidado>(sp => sp.GetRequiredService<ConsolidadoRepository>());
        services.AddSingleton<IProjecaoConsolidado>(sp => sp.GetRequiredService<ConsolidadoRepository>());
        services.AddHostedService<ConsolidadoConsumer>();

        return services;
    }
}
