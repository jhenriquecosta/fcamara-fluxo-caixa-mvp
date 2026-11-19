using System.Reflection;
using Cashflow.CrossCuttingConcerns.ExtensionMethods;

namespace Cashflow.Application.Decorators;

internal static class Mappings
{
    public static readonly Dictionary<Type, Type> AttributeToCommandHandler = new();
    public static readonly Dictionary<Type, Type> AttributeToQueryHandler = new();

    static Mappings()
    {
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
        {
            var mapping = type.GetCustomAttribute<MappingAttribute>(inherit: false);
            if (mapping is null)
                continue;

            if (type.HasInterface(typeof(ICommandHandler<>)))
                AttributeToCommandHandler[mapping.Type] = type;
            else if (type.HasInterface(typeof(IQueryHandler<,>)))
                AttributeToQueryHandler[mapping.Type] = type;
        }
    }
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class MappingAttribute : Attribute
{
    public required Type Type { get; init; }
}
