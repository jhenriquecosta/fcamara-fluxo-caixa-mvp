using Cashflow.Application;
using Cashflow.Infrastructure.Data;
using Cashflow.Modules.Consolidado.ConfigurationOptions;
using Cashflow.Modules.Consolidado.Commands;
using Cashflow.Modules.Consolidado.HostedServices;
using Cashflow.Modules.Consolidado.Observability;
using Cashflow.Modules.Consolidado.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Cashflow.Modules.Consolidado;

public static class ConsolidadoModuleServiceCollectionExtensions
{
    public static IServiceCollection AddConsolidadoModule(
        this IServiceCollection services, Action<ConsolidadoModuleOptions> configureOptions)
    {
        var settings = new ConsolidadoModuleOptions();
        configureOptions(settings);
        services.Configure(configureOptions);

        services.AddCashflowPostgres(settings.ConnectionStrings.Default);
        services.AddSingleton<ConsolidadoMetrics>();
        services.AddSingleton<ConsolidadoRepository>();
        services.AddSingleton<IConsultaConsolidado>(sp => sp.GetRequiredService<ConsolidadoRepository>());
        services.AddSingleton<IProjecaoConsolidado>(sp => sp.GetRequiredService<ConsolidadoRepository>());
        services.AddMessageHandlers(typeof(ConsolidadoModuleServiceCollectionExtensions).Assembly);
        services.AddTransient<ConsultarConsolidadoHandler>();
        services.AddTransient<ProjetarEventoHandler>();

        return services;
    }

    public static IServiceCollection AddHostedServicesConsolidadoModule(this IServiceCollection services)
    {
        services.AddHostedService<ConsolidadoConsumer>();
        return services;
    }

    public static Task ApplyConsolidadoSchemaAsync(
        this NpgsqlDataSource dataSource, CancellationToken ct = default) =>
        SqlSchema.ApplyAsync(dataSource, typeof(ConsolidadoModuleServiceCollectionExtensions).Assembly,
            "Cashflow.Modules.Consolidado.Repositories.Schema.sql", ct);
}
