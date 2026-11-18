using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Cashflow.Application;

public sealed class Dispatcher(IServiceProvider provider)
{
    public async Task DispatchAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(ICommandHandler<>).MakeGenericType(command.GetType());
        dynamic? handler = provider.GetService(handlerType)
            ?? throw new InvalidOperationException($"handler não registrado para {command.GetType().Name}");
        await handler.HandleAsync((dynamic)command, cancellationToken);
    }

    public async Task<TResult> DispatchAsync<TResult>(ICommand command, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));
        dynamic? handler = provider.GetService(handlerType)
            ?? throw new InvalidOperationException($"handler não registrado para {command.GetType().Name}");
        return await handler.HandleAsync((dynamic)command, cancellationToken);
    }

    public async Task<TResult> DispatchAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResult));
        dynamic? handler = provider.GetService(handlerType)
            ?? throw new InvalidOperationException($"handler não registrado para {query.GetType().Name}");
        return await handler.HandleAsync((dynamic)query, cancellationToken);
    }
}

public static class ApplicationServiceCollectionExtensions
{
    /// <summary>Registra o Dispatcher e todos os handlers do assembly do módulo, como a referência faz.</summary>
    public static IServiceCollection AddMessageHandlers(this IServiceCollection services, Assembly assembly)
    {
        var tipos = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .ToList();

        foreach (var tipo in tipos)
        {
            foreach (var contrato in tipo.GetInterfaces()
                         .Where(i => i.IsGenericType && (
                             i.GetGenericTypeDefinition() == typeof(ICommandHandler<>) ||
                             i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>) ||
                             i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>) ||
                             i.GetGenericTypeDefinition() == typeof(Domain.Events.IDomainEventHandler<>))))
            {
                services.AddTransient(tipo);
                services.AddTransient(contrato, tipo);
            }
        }

        return services;
    }

    public static IServiceCollection AddCashflowApplication(this IServiceCollection services)
    {
        services.AddTransient<Dispatcher>();
        return services;
    }
}
