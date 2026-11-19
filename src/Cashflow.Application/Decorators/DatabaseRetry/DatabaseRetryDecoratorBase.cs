using System.Data.Common;

namespace Cashflow.Application.Decorators.DatabaseRetry;

/// <summary>
/// Repete a execução quando o banco devolve uma falha transitória. A referência casa a
/// mensagem do SQL Server; aqui vale o contrato do ADO.NET (<see cref="DbException.IsTransient"/>),
/// que o Npgsql preenche — a Application continua sem conhecer o driver.
/// </summary>
public abstract class DatabaseRetryDecoratorBase(DatabaseRetryAttribute options)
{
    protected async Task<T> WrapExecutionAsync<T>(Func<Task<T>> action)
    {
        var executedTimes = 0;
        while (true)
        {
            try
            {
                executedTimes++;
                return await action();
            }
            catch (Exception ex) when (executedTimes < options.RetryTimes && IsTransient(ex))
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100 * executedTimes));
            }
        }
    }

    protected Task WrapExecutionAsync(Func<Task> action) =>
        WrapExecutionAsync(async () =>
        {
            await action();
            return true;
        });

    private static bool IsTransient(Exception exception) =>
        exception is DbException { IsTransient: true }
        || exception.InnerException is DbException { IsTransient: true };
}
