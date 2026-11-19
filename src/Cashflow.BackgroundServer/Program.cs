using Cashflow.BackgroundServer.ConfigurationOptions;
using Cashflow.Contracts;
using Cashflow.Domain.Infrastructure.MessageBrokers;
using Cashflow.Infrastructure;
using Cashflow.Infrastructure.Web;
using Cashflow.Modules.Consolidado.MessageBusConsumers;
using Cashflow.Modules.Lancamentos.Observability;

// Os trabalhos de fundo dos módulos, como o BackgroundServer da referência: o publicador do
// outbox (Lançamentos) e o consumidor do barramento (Consolidado). Expõe só /health e /metrics.
var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.AddCashflowInfrastructure("background-server", LancamentosMetrics.MeterName);

var appSettings = new AppSettings();
configuration.Bind(appSettings);

var services = builder.Services;
services.AddApplicationServices();
services.AddMessageBus();

if (appSettings.Modules.Lancamentos.Enabled)
{
    services.AddLancamentosModule(opt => configuration.GetSection("Modules:Lancamentos").Bind(opt));
    services.AddMessageBusSender<LancamentoEventPayload>(appSettings.MessageBroker);
    services.AddHostedServicesLancamentosModule();
}

if (appSettings.Modules.Consolidado.Enabled)
{
    services.AddConsolidadoModule(opt => configuration.GetSection("Modules:Consolidado").Bind(opt));
    services.AddMessageBusReceiver<LancamentoEventConsumer, LancamentoEventPayload>(appSettings.MessageBroker);
    services.AddHostedServicesConsolidadoModule();
}

var app = builder.Build();

app.UseCashflowWeb();
app.MapCashflowEndpoints();

app.Run();
