namespace Cashflow.Modules.Consolidado.ConfigurationOptions;

/// <summary>A seção <c>Modules:Consolidado</c>: se o módulo liga neste host e onde fica o banco dele.</summary>
public sealed class ConsolidadoModuleOptions
{
    /// <summary>A chave do módulo na configuração (<c>Modules:Consolidado</c>) e no data source.</summary>
    public const string Key = "Consolidado";

    /// <summary>Liga o módulo no host. O mesmo WebAPI sobe como <c>consolidado-api</c> só com ele ligado.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>As conexões do módulo.</summary>
    public ConnectionStringsOptions ConnectionStrings { get; set; } = new();
}
