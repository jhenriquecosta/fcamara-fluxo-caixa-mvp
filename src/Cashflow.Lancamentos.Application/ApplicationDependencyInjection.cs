using Microsoft.Extensions.DependencyInjection;

namespace Cashflow.Lancamentos.Application;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddLancamentosApplication(this IServiceCollection services)
    {
        services.AddSingleton<RegistrarLancamentoHandler>();
        services.AddSingleton<EstornarLancamentoHandler>();
        return services;
    }
}
