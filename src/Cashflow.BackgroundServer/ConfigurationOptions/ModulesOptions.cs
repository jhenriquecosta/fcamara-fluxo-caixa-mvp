using Cashflow.Modules.Consolidado.ConfigurationOptions;
using Cashflow.Modules.Lancamentos.ConfigurationOptions;

namespace Cashflow.BackgroundServer.ConfigurationOptions;

/// <summary>A seção <c>Modules</c>: quais módulos este host liga. O deploy decide, não o código.</summary>
public sealed class ModulesOptions
{
    /// <summary>O módulo de Lançamentos.</summary>
    public LancamentosModuleOptions Lancamentos { get; set; } = new();

    /// <summary>O módulo de Consolidado.</summary>
    public ConsolidadoModuleOptions Consolidado { get; set; } = new();
}
