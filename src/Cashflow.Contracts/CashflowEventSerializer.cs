using System.Text.Json;

namespace Cashflow.Contracts;

public static class CashflowEventSerializer
{
    public static string Serialize<T>(CashflowEvent<T> evento) =>
        JsonSerializer.Serialize(evento, CashflowJson.Options);

    public static CashflowEvent<T>? Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<CashflowEvent<T>>(json, CashflowJson.Options);
}
