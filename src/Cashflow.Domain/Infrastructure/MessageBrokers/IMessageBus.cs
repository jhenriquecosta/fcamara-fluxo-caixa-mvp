namespace Cashflow.Domain.Infrastructure.MessageBrokers;

/// <summary>
/// A única porta dos módulos para o barramento. O módulo não conhece Kafka (o ArchTests proíbe):
/// envia e recebe tipos do contrato, e a Infrastructure decide o broker, o tópico e o envelope.
/// </summary>
public interface IMessageBus
{
    /// <summary>Publica a mensagem no tópico configurado para o tipo <typeparamref name="T"/>.</summary>
    Task SendAsync<T>(T message, MetaData? metaData = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consome o tópico de <typeparamref name="T"/> até o cancelamento, entregando cada mensagem a
    /// todos os <see cref="IMessageBusConsumer{TConsumer,T}"/> registrados, cada uma num escopo de DI novo.
    /// </summary>
    Task ReceiveAsync<TConsumer, T>(CancellationToken cancellationToken = default);

    /// <summary>Entrega um evento lido do outbox ao <see cref="IOutBoxEventPublisher"/> que o sabe publicar.</summary>
    Task SendAsync(PublishingOutBoxEvent outbox, CancellationToken cancellationToken = default);
}
