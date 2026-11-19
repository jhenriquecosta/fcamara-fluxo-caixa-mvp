using Cashflow.Infrastructure.MessageBrokers;

namespace Cashflow.BackgroundServer.ConfigurationOptions;

/// <summary>A configuração do host, lida antes de montar o DI para decidir o que ligar.</summary>
public sealed class AppSettings
{
    /// <summary>O nome nos logs e nas métricas.</summary>
    public string ServiceName { get; set; } = "background-server";

    /// <summary>O broker: o host decide quem publica e quem consome.</summary>
    public MessageBrokerOptions MessageBroker { get; set; } = new();

    /// <summary>Os módulos ligados neste host.</summary>
    public ModulesOptions Modules { get; set; } = new();
}
