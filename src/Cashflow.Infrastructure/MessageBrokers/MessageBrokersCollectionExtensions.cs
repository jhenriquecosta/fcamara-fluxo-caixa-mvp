using Cashflow.CrossCuttingConcerns.Exceptions;
using Cashflow.Domain.Infrastructure.MessageBrokers;
using Cashflow.Infrastructure.MessageBrokers;
using Cashflow.Infrastructure.MessageBrokers.Kafka;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Liga o tipo de mensagem ao broker. É o host que decide quem publica e quem consome, então este
/// registro fica no <c>Program</c> do host, não no módulo. Falta de configuração falha na subida.
/// </summary>
public static class MessageBrokersCollectionExtensions
{
    /// <summary>Registra a publicação de <typeparamref name="T"/> no broker configurado.</summary>
    public static IServiceCollection AddMessageBusSender<T>(this IServiceCollection services, MessageBrokerOptions options)
    {
        EnsureKafka(options);
        return services.AddKafkaSender<T>(options.Kafka);
    }

    /// <summary>Registra o consumo de <typeparamref name="T"/> pelo <typeparamref name="TConsumer"/> no broker configurado.</summary>
    public static IServiceCollection AddMessageBusReceiver<TConsumer, T>(this IServiceCollection services, MessageBrokerOptions options)
    {
        EnsureKafka(options);
        return services.AddKafkaReceiver<TConsumer, T>(options.Kafka);
    }

    /// <summary>Um <see cref="KafkaSender{T}"/> singleton para o tópico de <typeparamref name="T"/>.</summary>
    public static IServiceCollection AddKafkaSender<T>(this IServiceCollection services, KafkaOptions options)
    {
        services.AddSingleton<IMessageSender<T>>(new KafkaSender<T>(
            Required(options.BootstrapServers, "MessageBroker:Kafka:BootstrapServers"),
            TopicFor<T>(options.Topics, "Topics")));
        return services;
    }

    /// <summary>Um <see cref="KafkaReceiver{TConsumer,T}"/> para o tópico e a DLT de <typeparamref name="T"/>.</summary>
    public static IServiceCollection AddKafkaReceiver<TConsumer, T>(this IServiceCollection services, KafkaOptions options)
    {
        var bootstrap = Required(options.BootstrapServers, "MessageBroker:Kafka:BootstrapServers");
        var topic = TopicFor<T>(options.Topics, "Topics");
        var deadLetterTopic = TopicFor<T>(options.DeadLetterTopics, "DeadLetterTopics");
        var groupId = Required(options.GroupId, "MessageBroker:Kafka:GroupId");

        services.AddTransient<IMessageReceiver<TConsumer, T>>(sp => new KafkaReceiver<TConsumer, T>(
            bootstrap, topic, deadLetterTopic, groupId,
            sp.GetRequiredService<MessagingMetrics>(),
            sp.GetRequiredService<ILogger<KafkaReceiver<TConsumer, T>>>()));
        return services;
    }

    private static void EnsureKafka(MessageBrokerOptions options)
    {
        if (!options.UsesKafka())
            throw new CashflowConfigurationException($"MessageBroker:Provider '{options.Provider}' não suportado");
    }

    private static string Required(string value, string key) =>
        string.IsNullOrWhiteSpace(value) ? throw new CashflowConfigurationException($"{key} não configurado") : value;

    private static string TopicFor<T>(Dictionary<string, string> topics, string section) =>
        topics.TryGetValue(typeof(T).Name, out var topic) && !string.IsNullOrWhiteSpace(topic)
            ? topic
            : throw new CashflowConfigurationException($"MessageBroker:Kafka:{section}:{typeof(T).Name} não configurado");
}
