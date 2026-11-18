using Cashflow.CrossCuttingConcerns.DateTimes;

namespace Cashflow.Infrastructure.DateTimes;

/// <summary>Sobre o <see cref="TimeProvider"/> do .NET: os testes trocam o relógio sem tocar no domínio.</summary>
public sealed class DateTimeProvider(TimeProvider timeProvider) : IDateTimeProvider
{
    public DateTime Now => timeProvider.GetLocalNow().DateTime;

    public DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    public DateTimeOffset OffsetNow => timeProvider.GetLocalNow();

    public DateTimeOffset OffsetUtcNow => timeProvider.GetUtcNow();
}
