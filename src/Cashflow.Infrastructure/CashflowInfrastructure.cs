using Cashflow.Infrastructure.Configuration;
using Cashflow.Infrastructure.DateTimes;
using Cashflow.Infrastructure.Logging;
using Cashflow.Infrastructure.Monitoring;
using Cashflow.Infrastructure.Security;
using Microsoft.AspNetCore.Builder;

namespace Cashflow.Infrastructure;

public static class CashflowInfrastructure
{
    /// <summary>O que todo host compartilha: configuração padrão, Serilog, relógio, autenticação e métricas.</summary>
    public static WebApplicationBuilder AddCashflowInfrastructure(
        this WebApplicationBuilder builder, string serviceName, params string[] meterNames)
    {
        CashflowDefaults.Aplicar(builder.Configuration);
        builder.Host.UseCashflowLogging(serviceName);
        builder.Services.AddDateTimeProvider(builder.Configuration);
        builder.Services.AddCashflowAuth(builder.Configuration);
        builder.Services.AddMonitoringServices(meterNames);
        return builder;
    }
}
