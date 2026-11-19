using Cashflow.Infrastructure;
using Cashflow.Infrastructure.Web;
using Cashflow.Modules.Consolidado.Endpoints;
using Cashflow.Modules.Lancamentos.Endpoints;
using Cashflow.Modules.Lancamentos.Observability;
using Cashflow.WebAPI.ConfigurationOptions;
using Scalar.AspNetCore;

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
services.AddOpenApi(options => options.AddCashflowOpenApiTransforms(configuration));

if (appSettings.Modules.Lancamentos.Enabled)
    services.AddLancamentosModule(opt => configuration.GetSection("Modules:Lancamentos").Bind(opt));

if (appSettings.Modules.Consolidado.Enabled)
    services.AddConsolidadoModule(opt => configuration.GetSection("Modules:Consolidado").Bind(opt));

var app = builder.Build();

app.UseCashflowWeb();
app.MapCashflowEndpoints();

// O contrato (ADR-0013) descreve só o que este deploy mapeia: as rotas condicionais abaixo.
app.MapOpenApi();

// A UI interativa é conveniência de desenvolvimento; fora dele, a rota não existe.
if (app.Environment.IsDevelopment())
    app.MapScalarApiReference(options => options.AddDocument("v1"));

if (appSettings.Modules.Lancamentos.Enabled)
    app.MapLancamentosEndpoints();

if (appSettings.Modules.Consolidado.Enabled)
    app.MapConsolidadoEndpoints();

app.Run();
