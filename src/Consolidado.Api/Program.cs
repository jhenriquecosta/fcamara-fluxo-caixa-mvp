using Cashflow.Common;
using Cashflow.Common.Web;
using Cashflow.Consolidado.Api.Endpoints;
using Cashflow.Consolidado.Application;
using Cashflow.Consolidado.Infrastructure;
using Cashflow.Consolidado.Infrastructure.Observability;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.AddCashflowCommon("consolidado", ConsolidadoMetrics.MeterName);
builder.Services.AddConsolidadoApplication();
builder.Services.AddConsolidadoInfrastructure(builder.Configuration);

var app = builder.Build();

await app.Services.GetRequiredService<NpgsqlDataSource>().ApplyConsolidadoSchemaAsync();

app.UseCashflowWeb();
app.MapCashflowEndpoints();
app.MapConsolidadoEndpoints();

app.Run();

public partial class Program;
