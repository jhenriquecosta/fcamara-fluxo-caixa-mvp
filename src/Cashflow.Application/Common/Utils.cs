namespace Cashflow.Application;

internal static class Utils
{
    public static bool IsHandlerInterface(Type type)
    {
        if (!type.IsGenericType)
            return false;

        var definition = type.GetGenericTypeDefinition();
        return definition == typeof(ICommandHandler<>) || definition == typeof(IQueryHandler<,>);
    }
}
