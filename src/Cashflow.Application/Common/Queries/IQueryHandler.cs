namespace Cashflow.Application;

/// <summary>Responde uma <see cref="IQuery{TResult}"/> sem alterar estado.</summary>
/// <typeparam name="TQuery">A consulta respondida.</typeparam>
/// <typeparam name="TResult">O modelo devolvido.</typeparam>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    /// <summary>Responde a consulta.</summary>
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
