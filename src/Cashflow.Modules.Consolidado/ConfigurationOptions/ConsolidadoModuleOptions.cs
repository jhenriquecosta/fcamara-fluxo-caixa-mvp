namespace Cashflow.Modules.Consolidado.ConfigurationOptions;

public sealed class ConsolidadoModuleOptions
{
    /// <summary>A chave do módulo na configuração (<c>Modules:Consolidado</c>) e no data source.</summary>
    public const string Key = "Consolidado";

    /// <summary>Liga o módulo no host. O mesmo WebAPI sobe como <c>consolidado-api</c> só com ele ligado.</summary>
    public bool Enabled { get; set; } = true;

    public ConnectionStringsOptions ConnectionStrings { get; set; } = new();
}
