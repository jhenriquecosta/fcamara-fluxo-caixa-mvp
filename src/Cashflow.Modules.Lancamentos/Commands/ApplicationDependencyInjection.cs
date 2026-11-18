using Microsoft.Extensions.DependencyInjection;

namespace Cashflow.Modules.Lancamentos.Commands;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddLancamentosApplication(this IServiceCollection services)
    {
        services.AddSingleton<RegistrarLancamentoHandler>();
        services.AddSingleton<EstornarLancamentoHandler>();
        return services;
    }
}
