namespace Cashflow.Domain.Infrastructure.MessageBrokers;

/// <summary>
/// O ponto de entrada de um módulo para as mensagens que ele assina. Não contém regra: converte a
/// mensagem num comando e o despacha pelo <c>Dispatcher</c>.
/// </summary>
/// <typeparam name="TConsumer">O próprio consumidor; separa assinantes diferentes da mesma mensagem.</typeparam>
/// <typeparam name="T">O payload do contrato que ele consome.</typeparam>
public interface IMessageBusConsumer<TConsumer, in T>
{
    /// <summary>Trata uma mensagem. Lançar faz o receiver tentar de novo e, esgotadas as tentativas, mandar para a DLT.</summary>
    Task HandleAsync(T data, MetaData metaData, CancellationToken cancellationToken = default);
}
