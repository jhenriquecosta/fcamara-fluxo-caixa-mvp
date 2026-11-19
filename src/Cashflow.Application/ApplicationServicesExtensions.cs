using System.Reflection;
using Cashflow.Application;

namespace Microsoft.Extensions.DependencyInjection;

public static class ApplicationServicesExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<Dispatcher>();
        return services;
    }

    /// <summary>
    /// Registra cada <c>ICommandHandler</c>/<c>IQueryHandler</c> do assembly do módulo,
    /// já decorado, e os <c>IDomainEventHandler</c> que o <see cref="Dispatcher"/> despacha.
    /// </summary>
    public static IServiceCollection AddMessageHandlers(this IServiceCollection services, Assembly assembly)
    {
        foreach (var type in assembly.GetTypes().Where(t => t is { IsAbstract: false, IsInterface: false }))
        {
            var handlerInterfaces = type.GetInterfaces().Where(Utils.IsHandlerInterface).ToList();
            if (handlerInterfaces.Count == 0)
                continue;

            var factory = new HandlerFactory(type);
            foreach (var interfaceType in handlerInterfaces)
                services.AddTransient(interfaceType, provider => factory.Create(provider, interfaceType));
        }

        Dispatcher.RegisterEventHandlers(assembly, services);
        return services;
    }
}
