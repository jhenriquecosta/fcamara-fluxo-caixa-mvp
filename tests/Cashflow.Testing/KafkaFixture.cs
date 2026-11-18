using Testcontainers.Kafka;
using Xunit;

namespace Cashflow.Testing;

public sealed class KafkaFixture : IAsyncLifetime
{
    public const string Topico = "cashflow.lancamentos.v1";

    private readonly KafkaContainer _container = new KafkaBuilder("apache/kafka:4.0.0")
        .WithKRaft()
        .WithVendor(KafkaVendor.ApacheSoftwareFoundation)
        .Build();

    public string BootstrapServers => _container.GetBootstrapAddress();

    public async ValueTask InitializeAsync() => await _container.StartAsync();

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}
