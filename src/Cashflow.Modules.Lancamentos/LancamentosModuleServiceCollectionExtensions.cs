using Cashflow.Application;
using Cashflow.Infrastructure.Data;
using Cashflow.Modules.Lancamentos.ConfigurationOptions;
using Cashflow.Modules.Lancamentos.Commands;
using Cashflow.Modules.Lancamentos.HostedServices;
using Cashflow.Modules.Lancamentos.Observability;
using Cashflow.Modules.Lancamentos.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Cashflow.Modules.Lancamentos;

public static class LancamentosModuleServiceCollectionExtensions
{
    public static IServiceCollection AddLancamentosModule(
        this IServiceCollection services, Action<LancamentosModuleOptions> configureOptions)
    {
        var settings = new LancamentosModuleOptions();
        configureOptions(settings);
        services.Configure(configureOptions);

        services.AddCashflowPostgres(settings.ConnectionStrings.Default);
        services.AddSingleton<LancamentosMetrics>();
        services.AddSingleton<IRegistroDeLancamentos, LancamentoRepository>();
        services.AddSingleton<IPortDeIdempotencia, IdempotenciaStore>();
        services.AddMessageHandlers(typeof(LancamentosModuleServiceCollectionExtensions).Assembly);
        services.AddTransient<RegistrarLancamentoHandler>();
        services.AddTransient<EstornarLancamentoHandler>();

        return services;
    }

    /// <summary>Os hosted services ficam no host de fundo, como no ModularMonolith de referência.</summary>
    public static IServiceCollection AddHostedServicesLancamentosModule(this IServiceCollection services)
    {
        services.AddHostedService<PublishEventWorker>();
        return services;
    }

    public static Task ApplyLancamentosSchemaAsync(
        this NpgsqlDataSource dataSource, CancellationToken ct = default) =>
        SqlSchema.ApplyAsync(dataSource, typeof(LancamentosModuleServiceCollectionExtensions).Assembly,
            "Cashflow.Modules.Lancamentos.Repositories.Schema.sql", ct);
}
