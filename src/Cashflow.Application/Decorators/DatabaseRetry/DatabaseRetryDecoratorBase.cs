using System.Data.Common;

namespace Cashflow.Application.Decorators.DatabaseRetry;

/// <summary>
/// O laço de nova tentativa, comum aos decorators de comando e de consulta. A referência casa a
/// mensagem do SQL Server; aqui vale o contrato do ADO.NET (<see cref="DbException.IsTransient"/>),
/// que o Npgsql preenche, e a Application continua sem conhecer o driver.
/// </summary>
public abstract class DatabaseRetryDecoratorBase(DatabaseRetryAttribute options)
{
    /// <summary>
    /// Executa a ação; numa falha transitória espera 100 ms × tentativa e repete, até
    /// <see cref="DatabaseRetryAttribute.RetryTimes"/>. Qualquer outra falha sobe na hora.
    /// </summary>
    protected async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action)
    {
        var attempt = 0;
        while (true)
        {
            try
            {
                attempt++;
                return await action();
            }
            catch (Exception ex) when (attempt < options.RetryTimes && IsTransient(ex))
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100 * attempt));
            }
        }
    }

    /// <inheritdoc cref="ExecuteWithRetryAsync{T}"/>
    protected Task ExecuteWithRetryAsync(Func<Task> action) =>
        ExecuteWithRetryAsync(async () =>
        {
            await action();
            return true;
        });

    private static bool IsTransient(Exception exception) =>
        exception is DbException { IsTransient: true }
        || exception.InnerException is DbException { IsTransient: true };
}
