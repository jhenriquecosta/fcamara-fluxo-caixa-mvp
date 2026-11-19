using System.Reflection;
using Cashflow.Domain.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Cashflow.Application;

/// <summary>
/// A porta de entrada dos casos de uso. Endpoints, workers e consumidores não conhecem handler
/// concreto (o ArchTests proíbe): entregam o comando ou a consulta aqui, e o Dispatcher resolve o
/// handler já decorado no escopo atual. Também despacha os eventos de domínio dentro da transação do comando.
/// </summary>
public sealed class Dispatcher(IServiceProvider provider)
{
    private static readonly List<Type> RegisteredEventHandlers = new();
    private static readonly Lock RegistrationLock = new();

    /// <summary>Registra no DI e no catálogo todo <see cref="IDomainEventHandler{T}"/> do assembly do módulo.</summary>
    public static void RegisterEventHandlers(Assembly assembly, IServiceCollection services)
    {
        var types = assembly.GetTypes()
            .Where(x => x.GetInterfaces().Any(y =>
                y.IsGenericType && y.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>)))
            .ToList();

        foreach (var type in types)
            services.AddTransient(type);

        lock (RegistrationLock)
            RegisteredEventHandlers.AddRange(types.Except(RegisteredEventHandlers).ToList());
    }

    /// <summary>Executa o comando pelo seu handler. Comando sem handler registrado é erro de montagem, e lança.</summary>
    public async Task DispatchAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(ICommandHandler<>).MakeGenericType(command.GetType());
        dynamic handler = provider.GetService(handlerType)
            ?? throw new InvalidOperationException($"nenhum handler registrado para {command.GetType().Name}");
        await handler.HandleAsync((dynamic)command, cancellationToken);
    }

    /// <summary>Responde a consulta pelo seu handler.</summary>
    public async Task<T> DispatchAsync<T>(IQuery<T> query, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(T));
        dynamic handler = provider.GetService(handlerType)
            ?? throw new InvalidOperationException($"nenhum handler registrado para {query.GetType().Name}");
        Task<T> result = handler.HandleAsync((dynamic)query, cancellationToken);
        return await result;
    }

    /// <summary>
    /// Entrega o evento de domínio a todos os handlers que o tratam, em sequência e no mesmo escopo:
    /// eles participam da transação do comando que despachou o evento.
    /// </summary>
    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        foreach (var handlerType in RegisteredEventHandlers.Where(t => HandlesEvent(t, domainEvent.GetType())))
        {
            dynamic handler = provider.GetRequiredService(handlerType);
            await handler.HandleAsync((dynamic)domainEvent, cancellationToken);
        }
    }

    private static bool HandlesEvent(Type handlerType, Type eventType) =>
        handlerType.GetInterfaces().Any(x =>
            x.IsGenericType
            && x.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>)
            && x.GenericTypeArguments[0] == eventType);
}
