namespace Cashflow.WebAPI.ConfigurationOptions;

/// <summary>A configuração do host, lida antes de montar o DI para decidir o que ligar.</summary>
public sealed class AppSettings
{
    /// <summary>O nome nos logs e nas métricas: <c>lancamentos</c> ou <c>consolidado</c> conforme o deploy.</summary>
    public string ServiceName { get; set; } = "webapi";

    /// <summary>Os módulos ligados neste host.</summary>
    public ModulesOptions Modules { get; set; } = new();
}
