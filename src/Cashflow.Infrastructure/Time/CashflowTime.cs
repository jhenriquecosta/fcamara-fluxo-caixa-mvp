using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cashflow.Infrastructure.Time;

public static class CashflowTime
{
    public const string FusoPadrao = "America/Sao_Paulo";

    public static IServiceCollection AddCashflowTime(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(_ => TimeZoneInfo.FindSystemTimeZoneById(
            configuration["TimeZone:Business"] ?? FusoPadrao));
        return services;
    }
}
