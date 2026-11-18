using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Cashflow.Domain.Infrastructure.MessageBrokers;

public sealed class MessageBus(IServiceProvider serviceProvider) : IMessageBus
{
    private static readonly List<Type> Consumers = new();
    private static readonly Dictionary<string, List<Type>> OutboxEventHandlers = new();
    private static readonly Lock Gate = new();

    internal static void AddConsumers(Assembly assembly, IServiceCollection services)
    {
        var types = assembly.GetTypes()
            .Where(x => x.GetInterfaces().Any(y =>
                y.IsGenericType && y.GetGenericTypeDefinition() == typeof(IMessageBusConsumer<,>)))
            .ToList();

        foreach (var type in types)
            services.AddTransient(type);

        lock (Gate)
            Consumers.AddRange(types.Except(Consumers).ToList());
    }

    internal static void AddOutboxEventPublishers(Assembly assembly, IServiceCollection services)
    {
        var types = assembly.GetTypes()
            .Where(x => x.GetInterfaces().Any(y => y == typeof(IOutBoxEventPublisher)))
            .ToList();

        foreach (var type in types)
        {
            services.AddTransient(type);

            var eventTypes = (string[])type.GetMethod(nameof(IOutBoxEventPublisher.CanHandleEventTypes))!
                .Invoke(null, null)!;
            var eventSource = (string)type.GetMethod(nameof(IOutBoxEventPublisher.CanHandleEventSource))!
                .Invoke(null, null)!;

            lock (Gate)
            {
                foreach (var eventType in eventTypes)
                {
                    var key = eventSource + ":" + eventType;
                    if (!OutboxEventHandlers.TryGetValue(key, out var handlers))
                        OutboxEventHandlers[key] = handlers = new List<Type>();
                    if (!handlers.Contains(type))
                        handlers.Add(type);
                }
            }
        }
    }

    public Task SendAsync<T>(T message, MetaData? metaData = null, CancellationToken cancellationToken = default) =>
        serviceProvider.GetRequiredService<IMessageSender<T>>().SendAsync(message, metaData, cancellationToken);

    public Task ReceiveAsync<TConsumer, T>(Func<T, MetaData, Task> action, CancellationToken cancellationToken = default) =>
        serviceProvider.GetRequiredService<IMessageReceiver<TConsumer, T>>().ReceiveAsync(action, cancellationToken);

    public Task ReceiveAsync<TConsumer, T>(CancellationToken cancellationToken = default) =>
        serviceProvider.GetRequiredService<IMessageReceiver<TConsumer, T>>().ReceiveAsync(async (data, metaData) =>
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            foreach (var handlerType in Consumers.Where(CanHandle<TConsumer, T>))
            {
                var handler = (IMessageBusConsumer<TConsumer, T>)scope.ServiceProvider.GetRequiredService(handlerType);
                await handler.HandleAsync(data, metaData, cancellationToken);
            }
        }, cancellationToken);

    public async Task SendAsync(PublishingOutBoxEvent outbox, CancellationToken cancellationToken = default)
    {
        if (!OutboxEventHandlers.TryGetValue(outbox.EventSource + ":" + outbox.EventType, out var handlerTypes))
            throw new InvalidOperationException(
                $"nenhum IOutBoxEventPublisher para {outbox.EventSource}:{outbox.EventType}");

        foreach (var type in handlerTypes)
        {
            var handler = (IOutBoxEventPublisher)serviceProvider.GetRequiredService(type);
            await handler.HandleAsync(outbox, cancellationToken);
        }
    }

    private static bool CanHandle<TConsumer, T>(Type handlerType) =>
        handlerType.GetInterfaces().Any(x =>
            x.IsGenericType
            && x.GetGenericTypeDefinition() == typeof(IMessageBusConsumer<,>)
            && x.GenericTypeArguments[0] == typeof(TConsumer)
            && x.GenericTypeArguments[1] == typeof(T));
}

public static class MessageBusExtensions
{
    public static IServiceCollection AddMessageBusConsumers(this IServiceCollection services, Assembly assembly)
    {
        MessageBus.AddConsumers(assembly, services);
        return services;
    }

    public static IServiceCollection AddOutboxEventPublishers(this IServiceCollection services, Assembly assembly)
    {
        MessageBus.AddOutboxEventPublishers(assembly, services);
        return services;
    }

    public static IServiceCollection AddMessageBus(this IServiceCollection services)
    {
        services.AddTransient<IMessageBus, MessageBus>();
        return services;
    }
}
