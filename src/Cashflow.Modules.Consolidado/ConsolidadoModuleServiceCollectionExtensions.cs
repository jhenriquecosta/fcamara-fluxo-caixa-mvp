using System.Reflection;
using Cashflow.Contracts;
using Cashflow.Domain.Infrastructure.MessageBrokers;
using Cashflow.Infrastructure.HostedServices;
using Cashflow.Infrastructure.Persistence;
using Cashflow.Modules.Consolidado.ConfigurationOptions;
using Cashflow.Modules.Consolidado.MessageBusConsumers;
using Cashflow.Modules.Consolidado.Repositories;
using Npgsql;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConsolidadoModuleServiceCollectionExtensions
{
    public static IServiceCollection AddConsolidadoModule(
        this IServiceCollection services, Action<ConsolidadoModuleOptions> configureOptions)
    {
        var settings = new ConsolidadoModuleOptions();
        configureOptions(settings);
        services.Configure(configureOptions);

        services.AddModuleDataSource(ConsolidadoModuleOptions.Key, settings.ConnectionStrings.Default);
        services.AddScoped<ConsolidadoDbContext>();
        services.AddScoped<IConsolidadoRepository, ConsolidadoRepository>();

        services.AddMessageHandlers(Assembly.GetExecutingAssembly());

        return services;
    }

    /// <summary>O consumidor do barramento: carregado pelo BackgroundServer, nunca pela API.</summary>
    public static IServiceCollection AddHostedServicesConsolidadoModule(this IServiceCollection services)
    {
        services.AddMessageBusConsumers(Assembly.GetExecutingAssembly());
        services.AddHostedService<MessageBusConsumerBackgroundService<LancamentoEventConsumer, LancamentoEventPayload>>();
        return services;
    }

    public static Task MigrateConsolidadoDbAsync(this NpgsqlDataSource dataSource, CancellationToken ct = default) =>
        SqlSchema.ApplyAsync(dataSource, Assembly.GetExecutingAssembly(),
            "Cashflow.Modules.Consolidado.Repositories.Schema.sql", ct);
}
