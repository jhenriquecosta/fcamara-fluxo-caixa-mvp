namespace Cashflow.Modules.Lancamentos.ConfigurationOptions;

public sealed class LancamentosModuleOptions
{
    /// <summary>A chave do módulo na configuração (<c>Modules:Lancamentos</c>) e no data source.</summary>
    public const string Key = "Lancamentos";

    /// <summary>Liga o módulo no host. O mesmo WebAPI sobe como <c>lancamentos-api</c> só com ele ligado.</summary>
    public bool Enabled { get; set; } = true;

    public ConnectionStringsOptions ConnectionStrings { get; set; } = new();
}
