using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Compact;

namespace Cashflow.Common.Logging;

public static class CashflowLogging
{
    public static IHostBuilder UseCashflowLogging(this IHostBuilder host, string serviceName) =>
        host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("service", serviceName)
                .Enrich.WithProperty("env", context.HostingEnvironment.EnvironmentName)
                .WriteTo.Console(new CompactJsonFormatter())
                .WriteTo.File(new CompactJsonFormatter(), $"logs/{serviceName}-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 14,
                    fileSizeLimitBytes: 104857600,
                    rollOnFileSizeLimit: true);

            var elasticsearchUrl = context.Configuration["Serilog:ElasticsearchUrl"];
            if (!string.IsNullOrWhiteSpace(elasticsearchUrl))
            {
                configuration.WriteTo.Elasticsearch(
                    new Serilog.Sinks.Elasticsearch.ElasticsearchSinkOptions(new Uri(elasticsearchUrl))
                    {
                        AutoRegisterTemplate = false,
                        IndexFormat = "cashflow-logs-{0:yyyy.MM.dd}"
                    });
            }
        });
}
