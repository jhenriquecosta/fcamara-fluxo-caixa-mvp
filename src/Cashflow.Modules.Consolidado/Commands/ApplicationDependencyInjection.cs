using Microsoft.Extensions.DependencyInjection;

namespace Cashflow.Modules.Consolidado.Commands;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddConsolidadoApplication(this IServiceCollection services)
    {
        services.AddSingleton<ConsultarConsolidadoHandler>();
        services.AddSingleton<ProjetarEventoHandler>();
        return services;
    }
}
