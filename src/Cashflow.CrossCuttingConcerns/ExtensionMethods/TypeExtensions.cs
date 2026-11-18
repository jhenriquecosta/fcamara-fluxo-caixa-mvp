namespace Cashflow.CrossCuttingConcerns.ExtensionMethods;

public static class TypeExtensions
{
    public static bool HasInterface(this Type type, Type interfaceType) =>
        type.GetInterfaces().Any(x =>
            x == interfaceType || (x.IsGenericType && x.GetGenericTypeDefinition() == interfaceType));

    /// <summary>Fecha um tipo genérico aberto com os argumentos dados; um tipo já fechado volta como está.</summary>
    public static Type MakeGenericTypeSafe(this Type type, params Type[] typeArguments) =>
        type.IsGenericTypeDefinition ? type.MakeGenericType(typeArguments) : type;
}
