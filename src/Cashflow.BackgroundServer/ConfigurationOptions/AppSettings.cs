using Cashflow.Infrastructure.MessageBrokers;

namespace Cashflow.BackgroundServer.ConfigurationOptions;

public sealed class AppSettings
{
    public string ServiceName { get; set; } = "background-server";

    public MessageBrokerOptions MessageBroker { get; set; } = new();

    public ModulesOptions Modules { get; set; } = new();
}
