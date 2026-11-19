namespace Cashflow.Domain.Events;

/// <summary>
/// Um fato do domínio, despachado em processo e dentro da transação do comando que o produziu.
/// Não é a mensagem do barramento: quem a transforma em evento de integração é o outbox do módulo.
/// </summary>
public interface IDomainEvent;
