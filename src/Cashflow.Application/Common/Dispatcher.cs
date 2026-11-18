using System.Reflection;
using Cashflow.Domain.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Cashflow.Application;

public sealed class Dispatcher(IServiceProvider provider)
{
    private static readonly List<Type> EventHandlers = new();
    private static readonly Lock Gate = new();

    public static void RegisterEventHandlers(Assembly assembly, IServiceCollection services)
    {
        var types = assembly.GetTypes()
            .Where(x => x.GetInterfaces().Any(y =>
                y.IsGenericType && y.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>)))
            .ToList();

        foreach (var type in types)
            services.AddTransient(type);

        lock (Gate)
            EventHandlers.AddRange(types.Except(EventHandlers).ToList());
    }

    public async Task DispatchAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(ICommandHandler<>).MakeGenericType(command.GetType());
        dynamic handler = provider.GetService(handlerType)
            ?? throw new InvalidOperationException($"nenhum handler registrado para {command.GetType().Name}");
        await handler.HandleAsync((dynamic)command, cancellationToken);
    }

    public async Task<T> DispatchAsync<T>(IQuery<T> query, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(T));
        dynamic handler = provider.GetService(handlerType)
            ?? throw new InvalidOperationException($"nenhum handler registrado para {query.GetType().Name}");
        Task<T> result = handler.HandleAsync((dynamic)query, cancellationToken);
        return await result;
    }

    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        foreach (var handlerType in EventHandlers)
        {
            var canHandleEvent = handlerType.GetInterfaces().Any(x =>
                x.IsGenericType
                && x.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>)
                && x.GenericTypeArguments[0] == domainEvent.GetType());

            if (!canHandleEvent)
                continue;

            dynamic handler = provider.GetRequiredService(handlerType);
            await handler.HandleAsync((dynamic)domainEvent, cancellationToken);
        }
    }
}
