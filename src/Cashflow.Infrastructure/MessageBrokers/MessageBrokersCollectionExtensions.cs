using Cashflow.CrossCuttingConcerns.Exceptions;
using Cashflow.Domain.Infrastructure.MessageBrokers;
using Cashflow.Infrastructure.MessageBrokers;
using Cashflow.Infrastructure.MessageBrokers.Kafka;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection;

public static class MessageBrokersCollectionExtensions
{
    public static IServiceCollection AddKafkaSender<T>(this IServiceCollection services, KafkaOptions options)
    {
        services.AddSingleton<IMessageSender<T>>(new KafkaSender<T>(
            Exigir(options.BootstrapServers, "MessageBroker:Kafka:BootstrapServers"),
            Topico(options.Topics, typeof(T).Name, "Topics")));
        return services;
    }

    public static IServiceCollection AddKafkaReceiver<TConsumer, T>(this IServiceCollection services, KafkaOptions options)
    {
        var bootstrap = Exigir(options.BootstrapServers, "MessageBroker:Kafka:BootstrapServers");
        var topico = Topico(options.Topics, typeof(T).Name, "Topics");
        var dlt = Topico(options.DeadLetterTopics, typeof(T).Name, "DeadLetterTopics");
        var grupo = Exigir(options.GroupId, "MessageBroker:Kafka:GroupId");

        services.AddTransient<IMessageReceiver<TConsumer, T>>(sp => new KafkaReceiver<TConsumer, T>(
            bootstrap, topico, dlt, grupo,
            sp.GetRequiredService<MessagingMetrics>(),
            sp.GetRequiredService<ILogger<KafkaReceiver<TConsumer, T>>>()));
        return services;
    }

    public static IServiceCollection AddMessageBusSender<T>(this IServiceCollection services, MessageBrokerOptions options)
    {
        if (!options.UsedKafka())
            throw new CashflowConfigurationException($"MessageBroker:Provider '{options.Provider}' não suportado");

        return services.AddKafkaSender<T>(options.Kafka);
    }

    public static IServiceCollection AddMessageBusReceiver<TConsumer, T>(this IServiceCollection services, MessageBrokerOptions options)
    {
        if (!options.UsedKafka())
            throw new CashflowConfigurationException($"MessageBroker:Provider '{options.Provider}' não suportado");

        return services.AddKafkaReceiver<TConsumer, T>(options.Kafka);
    }

    private static string Exigir(string valor, string chave) =>
        string.IsNullOrWhiteSpace(valor) ? throw new CashflowConfigurationException($"{chave} não configurado") : valor;

    private static string Topico(Dictionary<string, string> topicos, string tipo, string secao) =>
        topicos.TryGetValue(tipo, out var topico) && !string.IsNullOrWhiteSpace(topico)
            ? topico
            : throw new CashflowConfigurationException($"MessageBroker:Kafka:{secao}:{tipo} não configurado");
}
