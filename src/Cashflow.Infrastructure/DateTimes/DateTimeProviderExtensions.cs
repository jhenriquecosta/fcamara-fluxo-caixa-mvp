using Cashflow.CrossCuttingConcerns.DateTimes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cashflow.Infrastructure.DateTimes;

/// <summary>O registro do relógio e do fuso do negócio.</summary>
public static class DateTimeProviderExtensions
{
    /// <summary>O fuso do negócio quando a configuração não informa outro.</summary>
    public const string DefaultBusinessTimeZone = "America/Sao_Paulo";

    /// <summary>
    /// Registra o relógio e o <see cref="TimeZoneInfo"/> do negócio (<c>TimeZone:Business</c>). O dia
    /// de um lançamento é o de São Paulo, não o do servidor: 23h30 de 17/11 em São Paulo é 02h30 de 18/11 em UTC.
    /// </summary>
    public static IServiceCollection AddDateTimeProvider(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton(_ => TimeZoneInfo.FindSystemTimeZoneById(
            configuration["TimeZone:Business"] ?? DefaultBusinessTimeZone));
        return services;
    }
}
