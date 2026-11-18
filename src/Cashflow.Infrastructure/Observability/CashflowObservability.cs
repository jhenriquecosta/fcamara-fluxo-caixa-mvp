using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace Cashflow.Infrastructure.Observability;

public static class CashflowObservability
{
    public static IServiceCollection AddCashflowObservability(this IServiceCollection services, string meterName)
    {
        services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddMeter(meterName)
                .AddPrometheusExporter());

        return services;
    }
}
