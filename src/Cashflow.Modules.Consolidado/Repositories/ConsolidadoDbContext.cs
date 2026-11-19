using Cashflow.Infrastructure.Persistence;
using Cashflow.Modules.Consolidado.ConfigurationOptions;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Cashflow.Modules.Consolidado.Repositories;

/// <summary>A unidade de trabalho do módulo, ligada ao data source registrado com a chave <c>Consolidado</c>.</summary>
public sealed class ConsolidadoDbContext(
    [FromKeyedServices(ConsolidadoModuleOptions.Key)] NpgsqlDataSource dataSource) : DbContext(dataSource);
