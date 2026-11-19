namespace Cashflow.Application;

/// <summary>Reconhece as interfaces de handler do mecanismo de casos de uso.</summary>
internal static class HandlerInterfaces
{
    /// <summary>
    /// Diz se o tipo é um <see cref="ICommandHandler{TCommand}"/> ou <see cref="IQueryHandler{TQuery,TResult}"/>
    /// fechado. É o que separa, no construtor de um decorator, o handler decorado das demais dependências.
    /// </summary>
    public static bool IsHandlerInterface(Type type)
    {
        if (!type.IsGenericType)
            return false;

        var definition = type.GetGenericTypeDefinition();
        return definition == typeof(ICommandHandler<>) || definition == typeof(IQueryHandler<,>);
    }
}
