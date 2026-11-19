namespace Cashflow.Application.Decorators.DatabaseRetry;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DatabaseRetryAttribute(int retryTimes = 3) : Attribute
{
    public int RetryTimes { get; } = retryTimes;
}
