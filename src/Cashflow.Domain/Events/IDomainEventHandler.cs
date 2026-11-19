namespace Cashflow.Domain.Events;

/// <summary>
/// Reage a um evento de domínio dentro da mesma transação do comando. Se o handler falhar, o
/// comando inteiro desfaz, e é isso que mantém o lançamento e o seu evento de outbox juntos.
/// </summary>
/// <typeparam name="T">O evento tratado.</typeparam>
public interface IDomainEventHandler<in T>
    where T : IDomainEvent
{
    /// <summary>Trata o evento usando o escopo (e a transação) do comando que o despachou.</summary>
    Task HandleAsync(T domainEvent, CancellationToken cancellationToken = default);
}
