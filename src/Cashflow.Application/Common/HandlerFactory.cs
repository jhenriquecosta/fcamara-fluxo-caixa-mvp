using System.Reflection;
using Cashflow.Application.Decorators;
using Cashflow.CrossCuttingConcerns.ExtensionMethods;

namespace Cashflow.Application;

/// <summary>
/// Monta o handler envolvido pelos decorators que os atributos da classe pedem
/// (<c>[AuditLog]</c>, <c>[DatabaseRetry]</c>). O pipeline é declarado no handler, perto da regra,
/// e não espalhado pelo host.
/// </summary>
/// <remarks>
/// A ordem dos atributos é a ordem de fora para dentro: o primeiro atributo vira o decorator mais externo.
/// </remarks>
internal sealed class HandlerFactory
{
    private readonly List<Func<object?, Type, IServiceProvider, object>> _pipelineFromInnermost = new();

    public HandlerFactory(Type handlerType)
    {
        AddLayer(handlerType);
        AddDecoratorLayers(handlerType);
    }

    /// <summary>Constrói o pipeline inteiro para a interface pedida, do handler real até o decorator mais externo.</summary>
    public object Create(IServiceProvider provider, Type handlerInterfaceType)
    {
        object? current = null;
        foreach (var layer in _pipelineFromInnermost)
            current = layer(current, handlerInterfaceType, provider);
        return current!;
    }

    private void AddDecoratorLayers(Type handlerType)
    {
        var attributes = handlerType.GetCustomAttributes(inherit: false);
        for (var i = attributes.Length - 1; i >= 0; i--)
        {
            var attribute = attributes[i];
            if (DecoratorFor(handlerType, attribute.GetType()) is { } decoratorType)
                AddLayer(decoratorType, attribute);
        }
    }

    private static Type? DecoratorFor(Type handlerType, Type attributeType)
    {
        if (handlerType.HasInterface(typeof(ICommandHandler<>))
            && DecoratorMappings.CommandDecoratorByAttribute.TryGetValue(attributeType, out var commandDecorator))
            return commandDecorator;

        if (handlerType.HasInterface(typeof(IQueryHandler<,>))
            && DecoratorMappings.QueryDecoratorByAttribute.TryGetValue(attributeType, out var queryDecorator))
            return queryDecorator;

        return null;
    }

    private void AddLayer(Type layerType, object? attribute = null)
    {
        _pipelineFromInnermost.Add((inner, interfaceType, provider) =>
        {
            var ctor = layerType
                .MakeGenericTypeSafe(interfaceType.GenericTypeArguments)
                .GetConstructors()
                .Single();

            var arguments = ctor.GetParameters()
                .Select(p => ResolveArgument(p, inner, attribute, provider))
                .ToArray();

            return ctor.Invoke(arguments);
        });
    }

    /// <summary>
    /// O handler interno vai no parâmetro de interface de handler; o atributo vai no parâmetro do
    /// tipo dele (as opções do decorator); o resto vem do DI.
    /// </summary>
    private static object ResolveArgument(ParameterInfo parameter, object? inner, object? attribute, IServiceProvider provider)
    {
        var type = parameter.ParameterType;

        if (HandlerInterfaces.IsHandlerInterface(type))
            return inner!;

        if (type == attribute?.GetType())
            return attribute;

        return provider.GetService(type)
               ?? throw new ArgumentException($"tipo {type} não registrado para {parameter.Member.DeclaringType?.Name}");
    }
}
