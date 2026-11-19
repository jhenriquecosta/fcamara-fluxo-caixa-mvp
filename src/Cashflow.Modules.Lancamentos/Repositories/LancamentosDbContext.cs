using Cashflow.Infrastructure.Persistence;
using Cashflow.Modules.Lancamentos.ConfigurationOptions;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Cashflow.Modules.Lancamentos.Repositories;

/// <summary>A unidade de trabalho do módulo, ligada ao data source registrado com a chave <c>Lancamentos</c>.</summary>
public sealed class LancamentosDbContext(
    [FromKeyedServices(LancamentosModuleOptions.Key)] NpgsqlDataSource dataSource) : DbContext(dataSource);
