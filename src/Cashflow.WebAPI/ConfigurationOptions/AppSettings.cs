namespace Cashflow.WebAPI.ConfigurationOptions;

public sealed class AppSettings
{
    /// <summary>O nome nos logs e nas métricas: <c>lancamentos</c> ou <c>consolidado</c> conforme o deploy.</summary>
    public string ServiceName { get; set; } = "webapi";

    public ModulesOptions Modules { get; set; } = new();
}
