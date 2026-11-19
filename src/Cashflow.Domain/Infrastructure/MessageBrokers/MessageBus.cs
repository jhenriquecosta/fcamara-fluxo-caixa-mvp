using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Cashflow.Domain.Infrastructure.MessageBrokers;

/// <summary>
/// O <see cref="IMessageBus"/> da referência: roteia o envio para o <see cref="IMessageSender{T}"/>
/// do tipo, entrega o recebido a todos os <see cref="IMessageBusConsumer{TConsumer,T}"/> e leva cada
/// evento do outbox ao <see cref="IOutBoxEventPublisher"/> que o atende.
/// </summary>
/// <remarks>
/// Os consumidores e publicadores são descobertos por reflexão na subida, módulo a módulo, e guardados
/// em registros estáticos: o <see cref="MessageBus"/> é transiente, mas o catálogo vale para o processo.
/// </remarks>
public sealed class MessageBus(IServiceProvider serviceProvider) : IMessageBus
{
    private static readonly List<Type> RegisteredConsumers = new();
    private static readonly Dictionary<string, List<Type>> OutboxPublishersBySourceAndType = new();
    private static readonly Lock RegistrationLock = new();

    /// <summary>Registra no DI e no catálogo todo <see cref="IMessageBusConsumer{TConsumer,T}"/> do assembly.</summary>
    internal static void AddConsumers(Assembly assembly, IServiceCollection services)
    {
        var types = assembly.GetTypes()
            .Where(x => x.GetInterfaces().Any(y =>
                y.IsGenericType && y.GetGenericTypeDefinition() == typeof(IMessageBusConsumer<,>)))
            .ToList();

        foreach (var type in types)
            services.AddTransient(type);

        lock (RegistrationLock)
            RegisteredConsumers.AddRange(types.Except(RegisteredConsumers).ToList());
    }

    /// <summary>
    /// Registra todo <see cref="IOutBoxEventPublisher"/> do assembly, indexado por
    /// <c>origem:tipo</c> a partir dos membros estáticos que ele declara.
    /// </summary>
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

            lock (RegistrationLock)
            {
                foreach (var eventType in eventTypes)
                {
                    var key = OutboxKey(eventSource, eventType);
                    if (!OutboxPublishersBySourceAndType.TryGetValue(key, out var publishers))
                        OutboxPublishersBySourceAndType[key] = publishers = new List<Type>();
                    if (!publishers.Contains(type))
                        publishers.Add(type);
                }
            }
        }
    }

    /// <inheritdoc />
    public Task SendAsync<T>(T message, MetaData? metaData = null, CancellationToken cancellationToken = default) =>
        serviceProvider.GetRequiredService<IMessageSender<T>>().SendAsync(message, metaData, cancellationToken);

    /// <inheritdoc />
    public Task ReceiveAsync<TConsumer, T>(CancellationToken cancellationToken = default) =>
        serviceProvider.GetRequiredService<IMessageReceiver<TConsumer, T>>().ReceiveAsync(async (data, metaData) =>
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            foreach (var consumerType in RegisteredConsumers.Where(ConsumesMessage<TConsumer, T>))
            {
                var consumer = (IMessageBusConsumer<TConsumer, T>)scope.ServiceProvider.GetRequiredService(consumerType);
                await consumer.HandleAsync(data, metaData, cancellationToken);
            }
        }, cancellationToken);

    /// <inheritdoc />
    public async Task SendAsync(PublishingOutBoxEvent outbox, CancellationToken cancellationToken = default)
    {
        if (!OutboxPublishersBySourceAndType.TryGetValue(OutboxKey(outbox.EventSource, outbox.EventType), out var publisherTypes))
            throw new InvalidOperationException(
                $"nenhum IOutBoxEventPublisher para {outbox.EventSource}:{outbox.EventType}");

        foreach (var type in publisherTypes)
        {
            var publisher = (IOutBoxEventPublisher)serviceProvider.GetRequiredService(type);
            await publisher.HandleAsync(outbox, cancellationToken);
        }
    }

    private static string OutboxKey(string eventSource, string eventType) => eventSource + ":" + eventType;

    private static bool ConsumesMessage<TConsumer, T>(Type consumerType) =>
        consumerType.GetInterfaces().Any(x =>
            x.IsGenericType
            && x.GetGenericTypeDefinition() == typeof(IMessageBusConsumer<,>)
            && x.GenericTypeArguments[0] == typeof(TConsumer)
            && x.GenericTypeArguments[1] == typeof(T));
}

/// <summary>O registro do barramento no DI, chamado pelos módulos e pelos hosts.</summary>
public static class MessageBusExtensions
{
    /// <summary>Registra os consumidores de mensagens declarados no assembly do módulo.</summary>
    public static IServiceCollection AddMessageBusConsumers(this IServiceCollection services, Assembly assembly)
    {
        MessageBus.AddConsumers(assembly, services);
        return services;
    }

    /// <summary>Registra os publicadores de outbox declarados no assembly do módulo.</summary>
    public static IServiceCollection AddOutboxEventPublishers(this IServiceCollection services, Assembly assembly)
    {
        MessageBus.AddOutboxEventPublishers(assembly, services);
        return services;
    }

    /// <summary>Registra o próprio <see cref="IMessageBus"/>.</summary>
    public static IServiceCollection AddMessageBus(this IServiceCollection services)
    {
        services.AddTransient<IMessageBus, MessageBus>();
        return services;
    }
}
