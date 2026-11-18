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
        services.AddSingleton(_ => new NpgsqlDataSourceBuilder(
                configuration.GetConnectionString("Postgres")
                ?? throw new InvalidOperationException("ConnectionStrings:Postgres não configurada")).Build());

        services.AddSingleton<LancamentosMetrics>();
        services.AddSingleton<IRegistroDeLancamentos, LancamentoRepository>();
        services.AddSingleton<IPortDeIdempotencia, IdempotenciaStore>();
        services.AddHostedService<OutboxPublisher>();

        return services;
    }
}
