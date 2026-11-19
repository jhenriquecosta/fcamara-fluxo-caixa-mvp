using System.Reflection;
using Cashflow.Application;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>O registro do mecanismo de casos de uso no DI.</summary>
public static class ApplicationServicesExtensions
{
    /// <summary>Registra o <see cref="Dispatcher"/>, uma instância por escopo (por requisição ou por mensagem).</summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<Dispatcher>();
        return services;
    }

    /// <summary>
    /// Registra cada <see cref="ICommandHandler{TCommand}"/> e <see cref="IQueryHandler{TQuery,TResult}"/>
    /// do assembly do módulo já envolvido pelos seus decorators, e os <c>IDomainEventHandler</c> que o
    /// <see cref="Dispatcher"/> despacha. Cada módulo chama isto para o próprio assembly.
    /// </summary>
    public static IServiceCollection AddMessageHandlers(this IServiceCollection services, Assembly assembly)
    {
        foreach (var type in assembly.GetTypes().Where(t => t is { IsAbstract: false, IsInterface: false }))
        {
            var handlerInterfaces = type.GetInterfaces().Where(HandlerInterfaces.IsHandlerInterface).ToList();
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
