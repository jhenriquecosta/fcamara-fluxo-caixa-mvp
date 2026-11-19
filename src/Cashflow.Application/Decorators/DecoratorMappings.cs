using System.Reflection;
using Cashflow.CrossCuttingConcerns.ExtensionMethods;

namespace Cashflow.Application.Decorators;

/// <summary>
/// Qual decorator cada atributo pede, separado em comando e consulta. Montado uma vez, a partir do
/// <see cref="MappingAttribute"/> de cada decorator deste assembly.
/// </summary>
internal static class DecoratorMappings
{
    public static readonly Dictionary<Type, Type> CommandDecoratorByAttribute = new();
    public static readonly Dictionary<Type, Type> QueryDecoratorByAttribute = new();

    static DecoratorMappings()
    {
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
        {
            var mapping = type.GetCustomAttribute<MappingAttribute>(inherit: false);
            if (mapping is null)
                continue;

            if (type.HasInterface(typeof(ICommandHandler<>)))
                CommandDecoratorByAttribute[mapping.Type] = type;
            else if (type.HasInterface(typeof(IQueryHandler<,>)))
                QueryDecoratorByAttribute[mapping.Type] = type;
        }
    }
}

/// <summary>Liga um decorator ao atributo que o ativa num handler.</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class MappingAttribute : Attribute
{
    /// <summary>O atributo que, posto num handler, aplica o decorator marcado.</summary>
    public required Type Type { get; init; }
}
