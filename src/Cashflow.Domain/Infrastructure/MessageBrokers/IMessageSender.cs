namespace Cashflow.Domain.Infrastructure.MessageBrokers;

/// <summary>A escrita num tópico, implementada pela Infrastructure (Kafka). Os módulos não a usam direto.</summary>
/// <typeparam name="T">O payload publicado.</typeparam>
public interface IMessageSender<in T>
{
    /// <summary>Publica a mensagem, embrulhada no envelope do contrato, com a chave de partição do <paramref name="metaData"/>.</summary>
    Task SendAsync(T message, MetaData? metaData = null, CancellationToken cancellationToken = default);
}
