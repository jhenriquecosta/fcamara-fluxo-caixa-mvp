using Confluent.Kafka;

namespace Cashflow.Common.Messaging;

public static class CashflowKafka
{
    public static IProducer<string, string> CriarProdutor(KafkaOptions opcoes) =>
        new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = opcoes.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true
        }).Build();

    public static IProducer<string, string> CriarProdutorDlt(KafkaOptions opcoes) =>
        new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = opcoes.BootstrapServers,
            Acks = Acks.All
        }).Build();

    public static IConsumer<string, string> CriarConsumidor(KafkaOptions opcoes) =>
        new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = opcoes.BootstrapServers,
            GroupId = opcoes.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        }).Build();
}
