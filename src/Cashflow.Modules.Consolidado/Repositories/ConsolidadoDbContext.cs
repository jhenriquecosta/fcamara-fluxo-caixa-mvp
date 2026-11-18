using Cashflow.Infrastructure.Persistence;
using Cashflow.Modules.Consolidado.ConfigurationOptions;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Cashflow.Modules.Consolidado.Repositories;

public sealed class ConsolidadoDbContext(
    [FromKeyedServices(ConsolidadoModuleOptions.Key)] NpgsqlDataSource dataSource) : DbContext(dataSource);
