using Cashflow.Infrastructure.Configuration;
using Cashflow.Infrastructure.DateTimes;
using Cashflow.Infrastructure.Logging;
using Cashflow.Infrastructure.Monitoring;
using Cashflow.Infrastructure.Security;
using Microsoft.AspNetCore.Builder;

namespace Cashflow.Infrastructure;

/// <summary>A fundação que todo host do Cashflow liga numa linha, para que os hosts não divirjam entre si.</summary>
public static class CashflowInfrastructure
{
    /// <summary>
    /// Configuração padrão, logs estruturados (Serilog), relógio e fuso do negócio, autenticação JWT
    /// e métricas Prometheus.
    /// </summary>
    /// <param name="builder">O builder do host.</param>
    /// <param name="serviceName">O nome que aparece nos logs e nas métricas (<c>lancamentos</c>, <c>consolidado</c>...).</param>
    /// <param name="meterNames">Os medidores dos módulos que o host exporta, além dos de mensageria.</param>
    public static WebApplicationBuilder AddCashflowInfrastructure(
        this WebApplicationBuilder builder, string serviceName, params string[] meterNames)
    {
        CashflowDefaults.RegisterAsLowestPrioritySource(builder.Configuration);
        builder.Host.UseCashflowLogging(serviceName);
        builder.Services.AddDateTimeProvider(builder.Configuration);
        builder.Services.AddCashflowAuth(builder.Configuration);
        builder.Services.AddMonitoringServices(meterNames);
        return builder;
    }
}
