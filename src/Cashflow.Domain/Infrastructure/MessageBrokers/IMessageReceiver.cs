namespace Cashflow.Domain.Infrastructure.MessageBrokers;

/// <summary>A leitura de um tópico, implementada pela Infrastructure (Kafka). Os módulos não a usam direto.</summary>
/// <typeparam name="TConsumer">O consumidor dono da assinatura (vira o consumer group).</typeparam>
/// <typeparam name="T">O payload esperado no tópico.</typeparam>
public interface IMessageReceiver<TConsumer, out T>
{
    /// <summary>Lê o tópico até o cancelamento, chamando <paramref name="action"/> para cada mensagem.</summary>
    Task ReceiveAsync(Func<T, MetaData, Task> action, CancellationToken cancellationToken);
}
