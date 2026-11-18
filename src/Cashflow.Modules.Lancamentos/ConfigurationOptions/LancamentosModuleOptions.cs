namespace Cashflow.Modules.Lancamentos.ConfigurationOptions;

public sealed class LancamentosModuleOptions
{
    public ConnectionStringsOptions ConnectionStrings { get; set; } = new();
}

public sealed class ConnectionStringsOptions
{
    public string Default { get; set; } = string.Empty;
    public string MigrationsAssembly { get; set; } = string.Empty;
}
