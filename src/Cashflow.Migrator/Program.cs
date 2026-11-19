using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

// Aplica o esquema de cada módulo no banco dele, uma vez, antes de os hosts subirem —
// o papel do Migrator da referência (lá EF Migrations, aqui scripts SQL idempotentes).
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

await MigrarAsync("Lancamentos", (fonte, ct) => fonte.MigrateLancamentosDbAsync(ct));
await MigrarAsync("Consolidado", (fonte, ct) => fonte.MigrateConsolidadoDbAsync(ct));

return;

async Task MigrarAsync(string modulo, Func<NpgsqlDataSource, CancellationToken, Task> migrar)
{
    var connectionString = configuration[$"Modules:{modulo}:ConnectionStrings:Default"];
    if (string.IsNullOrWhiteSpace(connectionString))
        throw new InvalidOperationException($"Modules:{modulo}:ConnectionStrings:Default não configurada");

    await using var fonte = new NpgsqlDataSourceBuilder(connectionString).Build();
    await migrar(fonte, CancellationToken.None);
    Console.WriteLine($"esquema do módulo {modulo} aplicado");
}
