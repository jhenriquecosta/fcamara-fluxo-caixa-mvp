using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Compact;

namespace Cashflow.Infrastructure.Logging;

/// <summary>Os logs estruturados do sistema, iguais em todos os hosts (docs/ultron/agents/logging.md).</summary>
public static class CashflowLogging
{
    /// <summary>
    /// Serilog em JSON compacto no console (o que o container coleta) e em arquivo rotativo diário
    /// (14 dias, 100 MB). Com <c>Serilog:ElasticsearchUrl</c> configurada, envia também ao Elasticsearch.
    /// Todo evento leva <c>service</c> e <c>env</c>.
    /// </summary>
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
