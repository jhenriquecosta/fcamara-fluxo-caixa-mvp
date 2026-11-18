using Cashflow.Modules.Consolidado;
using Cashflow.Modules.Lancamentos;
using Microsoft.Extensions.Configuration;
using Npgsql;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

var lancamentos = configuration["Modules:Lancamentos:ConnectionStrings:Default"]
    ?? throw new InvalidOperationException("Modules:Lancamentos:ConnectionStrings:Default não configurada");
var consolidado = configuration["Modules:Consolidado:ConnectionStrings:Default"]
    ?? throw new InvalidOperationException("Modules:Consolidado:ConnectionStrings:Default não configurada");

await using var fonteLancamentos = new NpgsqlDataSourceBuilder(lancamentos).Build();
await using var fonteConsolidado = new NpgsqlDataSourceBuilder(consolidado).Build();

await fonteLancamentos.ApplyLancamentosSchemaAsync();
await fonteConsolidado.ApplyConsolidadoSchemaAsync();

Console.WriteLine("Esquemas dos módulos aplicados.");
