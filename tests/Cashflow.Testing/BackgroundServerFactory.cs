using Cashflow.BackgroundServer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Cashflow.Testing;

/// <summary>O <c>Cashflow.BackgroundServer</c>: o publicador do outbox e/ou o consumidor, conforme os módulos ligados.</summary>
public sealed class BackgroundServerFactory(
    string connectionString,
    string kafkaBootstrap,
    bool lancamentos = true,
    bool consolidado = true) : WebApplicationFactory<BackgroundServerEntryPoint>
{
    /// <summary>Sobe o host e os hosted services; o WebApplicationFactory só os inicia no primeiro acesso.</summary>
    public BackgroundServerFactory Iniciar()
    {
        _ = Server;
        return this;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var valores = HostSettings.Modulos(connectionString, lancamentos, consolidado);
        valores["MessageBroker__Kafka__BootstrapServers"] = kafkaBootstrap;
        HostSettings.Aplicar(valores);
        builder.UseEnvironment("Testing");
    }
}
