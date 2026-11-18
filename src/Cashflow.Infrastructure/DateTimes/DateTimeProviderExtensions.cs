using Cashflow.CrossCuttingConcerns.DateTimes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cashflow.Infrastructure.DateTimes;

public static class DateTimeProviderExtensions
{
    public const string FusoPadrao = "America/Sao_Paulo";

    /// <summary>O relógio e o fuso do negócio: o dia de um lançamento é o de São Paulo, não o do servidor.</summary>
    public static IServiceCollection AddDateTimeProvider(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton(_ => TimeZoneInfo.FindSystemTimeZoneById(
            configuration["TimeZone:Business"] ?? FusoPadrao));
        return services;
    }
}
