using Cashflow.Infrastructure.Persistence;
using Cashflow.Modules.Lancamentos.ConfigurationOptions;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Cashflow.Modules.Lancamentos.Repositories;

public sealed class LancamentosDbContext(
    [FromKeyedServices(LancamentosModuleOptions.Key)] NpgsqlDataSource dataSource) : DbContext(dataSource);
