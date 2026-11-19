using Cashflow.CrossCuttingConcerns.DateTimes;

namespace Cashflow.Infrastructure.DateTimes;

/// <summary>
/// O <see cref="IDateTimeProvider"/> sobre o <see cref="TimeProvider"/> do .NET. Os testes trocam o
/// <see cref="TimeProvider"/> registrado por um relógio fixo, sem tocar no domínio.
/// </summary>
public sealed class DateTimeProvider(TimeProvider timeProvider) : IDateTimeProvider
{
    /// <inheritdoc />
    public DateTimeOffset OffsetUtcNow => timeProvider.GetUtcNow();
}
