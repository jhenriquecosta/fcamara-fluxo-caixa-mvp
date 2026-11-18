using Testcontainers.Kafka;

namespace Cashflow.Lancamentos.Api.Tests;

public sealed class KafkaFixture : IAsyncLifetime
{
    private readonly KafkaContainer _container = new KafkaBuilder("apache/kafka:4.0.0")
        .WithKRaft()
        .WithVendor(KafkaVendor.ApacheSoftwareFoundation)
        .Build();

    public string BootstrapServers => _container.GetBootstrapAddress();

    public async ValueTask InitializeAsync() => await _container.StartAsync();

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}
