namespace Cashflow.CrossCuttingConcerns.ExtensionMethods;

/// <summary>Reflexão usada pela montagem dos handlers e dos decorators na subida do host.</summary>
public static class TypeExtensions
{
    /// <summary>
    /// Diz se o tipo implementa a interface, aceitando a definição genérica aberta
    /// (<c>typeof(ICommandHandler&lt;&gt;)</c>) como "qualquer fechamento dela".
    /// </summary>
    public static bool HasInterface(this Type type, Type interfaceType) =>
        type.GetInterfaces().Any(x =>
            x == interfaceType || (x.IsGenericType && x.GetGenericTypeDefinition() == interfaceType));

    /// <summary>Fecha um tipo genérico aberto com os argumentos dados; um tipo já fechado volta como está.</summary>
    public static Type MakeGenericTypeSafe(this Type type, params Type[] typeArguments) =>
        type.IsGenericTypeDefinition ? type.MakeGenericType(typeArguments) : type;
}
