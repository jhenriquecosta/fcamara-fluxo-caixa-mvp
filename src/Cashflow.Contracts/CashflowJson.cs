using System.Text.Json;

namespace Cashflow.Contracts;

/// <summary>As opções de JSON do sistema: as mesmas na API, no outbox e no barramento.</summary>
public static class CashflowJson
{
    /// <summary>Padrão web do .NET: camelCase, sem diferenciar maiúsculas na leitura.</summary>
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
