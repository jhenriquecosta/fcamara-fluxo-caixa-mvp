using Cashflow.Infrastructure.MessageBrokers;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;

namespace Cashflow.Infrastructure.Monitoring;

/// <summary>As métricas do host, expostas em <c>/metrics</c> para o Prometheus.</summary>
public static class MonitoringExtensions
{
    /// <summary>
    /// OpenTelemetry com as métricas do ASP.NET Core (latência e status HTTP, base dos SLOs), as de
    /// mensageria e os medidores que os módulos do host declaram.
    /// </summary>
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
