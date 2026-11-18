using Cashflow.Common.Auth;
using Cashflow.Common.Configuration;
using Cashflow.Common.Logging;
using Cashflow.Common.Messaging;
using Cashflow.Common.Observability;
using Cashflow.Common.Time;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cashflow.Common;

public static class CashflowCommon
{
    public static WebApplicationBuilder AddCashflowCommon(
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
