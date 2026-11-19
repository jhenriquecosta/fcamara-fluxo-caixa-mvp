using System.Reflection;
using Cashflow.Domain.Infrastructure.MessageBrokers;
using Cashflow.Infrastructure.Persistence;
using Cashflow.Modules.Lancamentos.ConfigurationOptions;
using Cashflow.Modules.Lancamentos.HostedServices;
using Cashflow.Modules.Lancamentos.Observability;
using Cashflow.Modules.Lancamentos.Repositories;
using Npgsql;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A porta de entrada do módulo para os hosts: o host liga o módulo com uma linha e não conhece nada
/// do que está dentro dele.
/// </summary>
public static class LancamentosModuleServiceCollectionExtensions
{
    /// <summary>
    /// O que a API precisa: o banco do módulo, os repositórios, as métricas e os casos de uso
    /// (handlers já decorados). Não sobe trabalho de fundo.
    /// </summary>
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

    /// <summary>Aplica o <c>Schema.sql</c> do módulo. Chamado pelo Migrator e pelas fixtures de teste.</summary>
    public static Task MigrateLancamentosDbAsync(this NpgsqlDataSource dataSource, CancellationToken ct = default) =>
        SqlSchema.ApplyAsync(dataSource, Assembly.GetExecutingAssembly(),
            "Cashflow.Modules.Lancamentos.Repositories.Schema.sql", ct);
}
