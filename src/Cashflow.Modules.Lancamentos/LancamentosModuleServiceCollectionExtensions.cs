using System.Reflection;
using Cashflow.Domain.Infrastructure.MessageBrokers;
using Cashflow.Infrastructure.Persistence;
using Cashflow.Modules.Lancamentos.ConfigurationOptions;
using Cashflow.Modules.Lancamentos.HostedServices;
using Cashflow.Modules.Lancamentos.Observability;
using Cashflow.Modules.Lancamentos.Repositories;
using Npgsql;

namespace Microsoft.Extensions.DependencyInjection;

public static class LancamentosModuleServiceCollectionExtensions
{
    public static IServiceCollection AddLancamentosModule(
        this IServiceCollection services, Action<LancamentosModuleOptions> configureOptions)
    {
        var settings = new LancamentosModuleOptions();
        configureOptions(settings);
        services.Configure(configureOptions);

        services.AddModuleDataSource(LancamentosModuleOptions.Key, settings.ConnectionStrings.Default);
        services.AddScoped<LancamentosDbContext>();

        services
            .AddScoped<ILancamentoRepository, LancamentoRepository>()
            .AddScoped<IOutboxEventRepository, OutboxEventRepository>()
            .AddScoped<IIdempotenciaRepository, IdempotenciaRepository>();

        services.AddSingleton<LancamentosMetrics>();
        services.AddMessageHandlers(Assembly.GetExecutingAssembly());

        return services;
    }

    /// <summary>O publicador do outbox: carregado pelo BackgroundServer, nunca pela API.</summary>
    public static IServiceCollection AddHostedServicesLancamentosModule(this IServiceCollection services)
    {
        services.AddOutboxEventPublishers(Assembly.GetExecutingAssembly());
        services.AddHostedService<PublishEventWorker>();
        return services;
    }

    public static Task MigrateLancamentosDbAsync(this NpgsqlDataSource dataSource, CancellationToken ct = default) =>
        SqlSchema.ApplyAsync(dataSource, Assembly.GetExecutingAssembly(),
            "Cashflow.Modules.Lancamentos.Repositories.Schema.sql", ct);
}
