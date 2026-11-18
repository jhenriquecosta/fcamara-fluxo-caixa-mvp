using Cashflow.Common.Auth;
using Cashflow.Common.Logging;
using Cashflow.Common.Observability;
using Cashflow.Common.Time;
using Microsoft.AspNetCore.Builder;

namespace Cashflow.Common;

public static class CashflowCommon
{
    public static WebApplicationBuilder AddCashflowCommon(
        this WebApplicationBuilder builder, string serviceName, string meterName)
    {
        builder.Host.UseCashflowLogging(serviceName);
        builder.Services.AddCashflowTime(builder.Configuration);
        builder.Services.AddCashflowAuth(builder.Configuration);
        builder.Services.AddCashflowObservability(meterName);
        return builder;
    }
}
