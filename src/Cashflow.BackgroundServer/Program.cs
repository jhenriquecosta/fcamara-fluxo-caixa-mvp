using Cashflow.Application;
using Cashflow.Infrastructure;
using Cashflow.Modules.Consolidado;
using Cashflow.Modules.Lancamentos;
using Cashflow.Modules.Lancamentos.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.AddCashflowInfrastructure("background", LancamentosMetrics.MeterName);
builder.Services.AddCashflowApplication();

builder.Services.AddLancamentosModule(opt =>
    builder.Configuration.GetSection("Modules:Lancamentos").Bind(opt));
builder.Services.AddHostedServicesLancamentosModule();

builder.Services.AddConsolidadoModule(opt =>
    builder.Configuration.GetSection("Modules:Consolidado").Bind(opt));
builder.Services.AddHostedServicesConsolidadoModule();

var app = builder.Build();

app.Run();
