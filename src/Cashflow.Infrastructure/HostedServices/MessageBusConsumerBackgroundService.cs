using Cashflow.Domain.Infrastructure.MessageBrokers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cashflow.Infrastructure.HostedServices;

/// <summary>
/// Mantém um consumidor do barramento rodando enquanto o host vive. Um por par
/// (consumidor, mensagem); o <c>BackgroundServer</c> sobe o do consolidado.
/// </summary>
public sealed class MessageBusConsumerBackgroundService<TConsumer, T>(
    IMessageBus messageBus,
    ILogger<MessageBusConsumerBackgroundService<TConsumer, T>> logger) : BackgroundService
{
    /// <summary>
    /// O receiver bloqueia no <c>Consume</c>, por isso roda fora da thread de subida para não travar o host
    /// (a referência chama o receive sem aguardar).
    /// </summary>
    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.Run(async () =>
        {
            logger.LogInformation("consumidor {Consumer} de {Message} iniciado", typeof(TConsumer).Name, typeof(T).Name);
            await messageBus.ReceiveAsync<TConsumer, T>(stoppingToken);
        }, stoppingToken);
}
