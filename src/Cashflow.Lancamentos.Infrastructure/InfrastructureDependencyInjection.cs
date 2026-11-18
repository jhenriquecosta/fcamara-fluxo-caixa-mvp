using Cashflow.Common.Data;
using Cashflow.Lancamentos.Domain.Ports;
using Cashflow.Lancamentos.Infrastructure.Observability;
using Cashflow.Lancamentos.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Cashflow.Lancamentos.Infrastructure;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddLancamentosInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCashflowPostgres(configuration);
        services.AddSingleton<LancamentosMetrics>();
        services.AddSingleton<IRegistroDeLancamentos, LancamentoRepository>();
        services.AddSingleton<IPortDeIdempotencia, IdempotenciaStore>();
        services.AddHostedService<OutboxPublisher>();
        return services;
    }

    public static Task ApplyLancamentosSchemaAsync(
        this NpgsqlDataSource dataSource, CancellationToken ct = default) =>
        SqlSchema.ApplyAsync(dataSource, typeof(InfrastructureDependencyInjection).Assembly,
            "Cashflow.Lancamentos.Infrastructure.Schema.sql", ct);
}
