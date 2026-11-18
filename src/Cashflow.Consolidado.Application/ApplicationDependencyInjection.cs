using Microsoft.Extensions.DependencyInjection;

namespace Cashflow.Consolidado.Application;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddConsolidadoApplication(this IServiceCollection services)
    {
        services.AddSingleton<ConsultarConsolidadoHandler>();
        services.AddSingleton<ProjetarEventoHandler>();
        return services;
    }
}
