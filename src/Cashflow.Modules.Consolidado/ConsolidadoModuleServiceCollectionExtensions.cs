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

/// <summary>
/// A porta de entrada do módulo para os hosts: o host liga o módulo com uma linha e não conhece nada
/// do que está dentro dele.
/// </summary>
public static class ConsolidadoModuleServiceCollectionExtensions
{
    /// <summary>O que a API precisa: o banco do módulo, o repositório e os casos de uso. Não sobe trabalho de fundo.</summary>
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

    /// <summary>Aplica o <c>Schema.sql</c> do módulo. Chamado pelo Migrator e pelas fixtures de teste.</summary>
    public static Task MigrateConsolidadoDbAsync(this NpgsqlDataSource dataSource, CancellationToken ct = default) =>
        SqlSchema.ApplyAsync(dataSource, Assembly.GetExecutingAssembly(),
            "Cashflow.Modules.Consolidado.Repositories.Schema.sql", ct);
}
