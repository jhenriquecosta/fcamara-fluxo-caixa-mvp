using Cashflow.Infrastructure.MessageBrokers;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;

namespace Cashflow.Infrastructure.Monitoring;

public static class MonitoringExtensions
{
    public static IServiceCollection AddMonitoringServices(this IServiceCollection services, params string[] meterNames)
    {
        services.AddSingleton<MessagingMetrics>();
        services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddMeter([.. meterNames, MessagingMetrics.MeterName])
                .AddPrometheusExporter());

        return services;
    }
}
