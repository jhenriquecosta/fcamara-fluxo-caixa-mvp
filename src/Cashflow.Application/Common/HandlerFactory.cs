using System.Reflection;
using Cashflow.Application.Decorators;
using Cashflow.CrossCuttingConcerns.ExtensionMethods;

namespace Cashflow.Application;

/// <summary>
/// Monta o handler com os decorators que os atributos da classe pedem
/// (<c>[AuditLog]</c>, <c>[DatabaseRetry]</c>): o pipeline é declarado no handler, não no host.
/// </summary>
internal sealed class HandlerFactory
{
    private readonly List<Func<object?, Type, IServiceProvider, object>> _pipeline = new();

    public HandlerFactory(Type type)
    {
        AddHandlerFactory(type);
        AddDecoratedFactories(type);
    }

    public object Create(IServiceProvider provider, Type handlerInterfaceType)
    {
        object? current = null;
        foreach (var factory in _pipeline)
            current = factory(current, handlerInterfaceType, provider);
        return current!;
    }

    private void AddDecoratedFactories(Type type)
    {
        var attributes = type.GetCustomAttributes(inherit: false);
        for (var i = attributes.Length - 1; i >= 0; i--)
        {
            var attribute = attributes[i];
            var attributeType = attribute.GetType();
            Type? decoratorType = null;

            var hasDecorator =
                (type.HasInterface(typeof(ICommandHandler<>))
                 && Mappings.AttributeToCommandHandler.TryGetValue(attributeType, out decoratorType))
                || (type.HasInterface(typeof(IQueryHandler<,>))
                    && Mappings.AttributeToQueryHandler.TryGetValue(attributeType, out decoratorType));

            if (hasDecorator)
                AddHandlerFactory(decoratorType!, attribute);
        }
    }

    private void AddHandlerFactory(Type handlerType, object? attribute = null)
    {
        _pipeline.Add((decorating, interfaceType, provider) =>
        {
            var ctor = handlerType
                .MakeGenericTypeSafe(interfaceType.GenericTypeArguments)
                .GetConstructors()
                .Single();

            var parameters = ctor.GetParameters()
                .Select(p => Resolve(p, decorating, attribute, provider))
                .ToArray();

            return ctor.Invoke(parameters);
        });
    }

    private static object Resolve(ParameterInfo parameter, object? current, object? attribute, IServiceProvider provider)
    {
        var type = parameter.ParameterType;

        if (Utils.IsHandlerInterface(type))
            return current!;

        if (type == attribute?.GetType())
            return attribute;

        return provider.GetService(type)
               ?? throw new ArgumentException($"tipo {type} não registrado para {parameter.Member.DeclaringType?.Name}");
    }
}
