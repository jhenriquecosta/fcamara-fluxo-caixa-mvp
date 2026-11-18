using Cashflow.Application;
using Cashflow.Infrastructure;
using Cashflow.Infrastructure.Web;
using Cashflow.Modules.Consolidado;
using Cashflow.Modules.Consolidado.Endpoints;
using Cashflow.Modules.Consolidado.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.AddCashflowInfrastructure("consolidado", ConsolidadoMetrics.MeterName);
builder.Services.AddCashflowApplication();
builder.Services.AddConsolidadoModule(opt =>
    builder.Configuration.GetSection("Modules:Consolidado").Bind(opt));

var app = builder.Build();

app.UseCashflowWeb();
app.MapCashflowEndpoints();
app.MapConsolidadoEndpoints();

app.Run();

public partial class Program;
