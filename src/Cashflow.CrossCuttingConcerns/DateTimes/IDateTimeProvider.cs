namespace Cashflow.CrossCuttingConcerns.DateTimes;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
