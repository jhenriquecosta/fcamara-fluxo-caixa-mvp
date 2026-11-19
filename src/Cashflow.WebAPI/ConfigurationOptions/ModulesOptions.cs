using Cashflow.Modules.Consolidado.ConfigurationOptions;
using Cashflow.Modules.Lancamentos.ConfigurationOptions;

namespace Cashflow.WebAPI.ConfigurationOptions;

public sealed class ModulesOptions
{
    public LancamentosModuleOptions Lancamentos { get; set; } = new();

    public ConsolidadoModuleOptions Consolidado { get; set; } = new();
}
