namespace Cashflow.Testing;

/// <summary>Um relógio parado no instante dado: o teste decide que horas são no servidor.</summary>
public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
