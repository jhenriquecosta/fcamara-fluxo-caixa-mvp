using Cashflow.Infrastructure;
using Cashflow.Infrastructure.Web;
using Cashflow.Modules.Consolidado.Endpoints;
using Cashflow.Modules.Lancamentos.Endpoints;
using Cashflow.Modules.Lancamentos.Observability;
using Cashflow.WebAPI.ConfigurationOptions;

// Um host, todos os módulos — como o WebAPI da referência. O deploy decide quais ligam
// (Modules:<X>:Enabled): o compose sobe este mesmo binário como lancamentos-api e como
// consolidado-api, e uma queda do consolidado não derruba os lançamentos (ADR-0001).
var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

var appSettings = new AppSettings();
configuration.Bind(appSettings);

builder.AddCashflowInfrastructure(appSettings.ServiceName, LancamentosMetrics.MeterName);

var services = builder.Services;
services.AddApplicationServices();

if (appSettings.Modules.Lancamentos.Enabled)
    services.AddLancamentosModule(opt => configuration.GetSection("Modules:Lancamentos").Bind(opt));

if (appSettings.Modules.Consolidado.Enabled)
    services.AddConsolidadoModule(opt => configuration.GetSection("Modules:Consolidado").Bind(opt));

var app = builder.Build();

app.UseCashflowWeb();
app.MapCashflowEndpoints();

if (appSettings.Modules.Lancamentos.Enabled)
    app.MapLancamentosEndpoints();

if (appSettings.Modules.Consolidado.Enabled)
    app.MapConsolidadoEndpoints();

app.Run();
