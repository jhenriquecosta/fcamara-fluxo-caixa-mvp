using Cashflow.Common;
using Cashflow.Common.Web;
using Cashflow.Lancamentos.Api.Endpoints;
using Cashflow.Lancamentos.Application;
using Cashflow.Lancamentos.Infrastructure;
using Cashflow.Lancamentos.Infrastructure.Observability;
using Cashflow.Lancamentos.Infrastructure.Persistence;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.AddCashflowCommon("lancamentos", LancamentosMetrics.MeterName);
builder.Services.AddLancamentosApplication();
builder.Services.AddLancamentosInfrastructure(builder.Configuration);

var app = builder.Build();

await Migrator.ApplyAsync(app.Services.GetRequiredService<NpgsqlDataSource>());

app.UseCashflowWeb();
app.MapCashflowEndpoints();
app.MapLancamentosEndpoints();

app.Run();

public partial class Program;
