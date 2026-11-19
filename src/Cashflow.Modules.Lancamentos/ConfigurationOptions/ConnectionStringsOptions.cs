namespace Cashflow.Modules.Lancamentos.ConfigurationOptions;

/// <summary>A seção <c>Modules:Lancamentos:ConnectionStrings</c>.</summary>
public sealed class ConnectionStringsOptions
{
    /// <summary>O PostgreSQL do módulo. Cada módulo tem o seu banco (ADR-0003).</summary>
    public string Default { get; set; } = string.Empty;
}
