using Cashflow.Infrastructure.Security;
using Cashflow.Infrastructure.Configuration;
using Cashflow.Infrastructure.Logging;
using Cashflow.Infrastructure.Messaging;
using Cashflow.Infrastructure.Observability;
using Cashflow.Infrastructure.Time;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cashflow.Infrastructure;

public static class CashflowInfrastructure
{
    public static WebApplicationBuilder AddCashflowInfrastructure(
        this WebApplicationBuilder builder, string serviceName, string meterName)
    {
        CashflowDefaults.Aplicar(builder.Configuration);
        builder.Host.UseCashflowLogging(serviceName);
        builder.Services.AddCashflowTime(builder.Configuration);
        builder.Services.AddCashflowAuth(builder.Configuration);
        builder.Services.AddCashflowObservability(meterName);
        builder.Services.AddSingleton(KafkaOptions.De(builder.Configuration));
        return builder;
    }
}
