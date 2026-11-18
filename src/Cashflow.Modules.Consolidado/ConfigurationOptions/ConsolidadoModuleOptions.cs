namespace Cashflow.Modules.Consolidado.ConfigurationOptions;

public sealed class ConsolidadoModuleOptions
{
    public ConnectionStringsOptions ConnectionStrings { get; set; } = new();
}

public sealed class ConnectionStringsOptions
{
    public string Default { get; set; } = string.Empty;
    public string MigrationsAssembly { get; set; } = string.Empty;
}
