namespace Cashflow.Application.Decorators.DatabaseRetry;

/// <summary>
/// Pede o decorator de nova tentativa: uma falha transitória do banco (conexão caída, failover)
/// repete o caso de uso inteiro em vez de virar erro para o cliente. Só vale para handler idempotente.
/// </summary>
/// <param name="retryTimes">Quantas execuções no total, contando a primeira.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DatabaseRetryAttribute(int retryTimes = 3) : Attribute
{
    /// <summary>Quantas execuções no total, contando a primeira.</summary>
    public int RetryTimes { get; } = retryTimes;
}
