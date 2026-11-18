using Cashflow.Application;
using Cashflow.Infrastructure;
using Cashflow.Infrastructure.Web;
using Cashflow.Modules.Lancamentos;
using Cashflow.Modules.Lancamentos.Endpoints;
using Cashflow.Modules.Lancamentos.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.AddCashflowInfrastructure("lancamentos", LancamentosMetrics.MeterName);
builder.Services.AddCashflowApplication();
builder.Services.AddLancamentosModule(opt =>
    builder.Configuration.GetSection("Modules:Lancamentos").Bind(opt));

var app = builder.Build();

app.UseCashflowWeb();
app.MapCashflowEndpoints();
app.MapLancamentosEndpoints();

app.Run();

public partial class Program;
