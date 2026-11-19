using Testcontainers.Kafka;
using Xunit;

namespace Cashflow.Testing;

/// <summary>Um Kafka 4 (KRaft, sem ZooKeeper) por assembly de teste, em container.</summary>
public sealed class KafkaFixture : IAsyncLifetime
{
    /// <summary>O tópico dos lançamentos, o mesmo do <c>Cashflow.Contracts.Topics</c>.</summary>
    public const string Topico = "cashflow.lancamentos.v1";

    private readonly KafkaContainer _container = new KafkaBuilder("apache/kafka:4.0.0")
        .WithKRaft()
        .WithVendor(KafkaVendor.ApacheSoftwareFoundation)
        .Build();

    /// <summary>O endereço do broker no container.</summary>
    public string BootstrapServers => _container.GetBootstrapAddress();

    public async ValueTask InitializeAsync() => await _container.StartAsync();

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}
