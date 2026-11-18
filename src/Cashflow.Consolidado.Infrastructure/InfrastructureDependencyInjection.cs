using Cashflow.Common.Data;
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
        services.AddCashflowPostgres(configuration);
        services.AddSingleton<ConsolidadoMetrics>();
        services.AddSingleton<ConsolidadoRepository>();
        services.AddSingleton<IConsultaConsolidado>(sp => sp.GetRequiredService<ConsolidadoRepository>());
        services.AddSingleton<IProjecaoConsolidado>(sp => sp.GetRequiredService<ConsolidadoRepository>());
        services.AddHostedService<ConsolidadoConsumer>();
        return services;
    }

    public static Task ApplyConsolidadoSchemaAsync(
        this NpgsqlDataSource dataSource, CancellationToken ct = default) =>
        SqlSchema.ApplyAsync(dataSource, typeof(InfrastructureDependencyInjection).Assembly,
            "Cashflow.Consolidado.Infrastructure.Schema.sql", ct);
}
