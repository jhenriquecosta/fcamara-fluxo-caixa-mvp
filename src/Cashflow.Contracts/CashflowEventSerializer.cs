using System.Text.Json;

namespace Cashflow.Contracts;

/// <summary>
/// A única forma de pôr e tirar um <see cref="CashflowEvent{T}"/> do fio. Quem publica e quem consome
/// usam a mesma serialização, então não há divergência de formato entre os lados.
/// </summary>
public static class CashflowEventSerializer
{
    /// <summary>O envelope em JSON camelCase (<see cref="CashflowJson.Options"/>).</summary>
    public static string Serialize<T>(CashflowEvent<T> evento) =>
        JsonSerializer.Serialize(evento, CashflowJson.Options);

    /// <summary>O envelope lido do JSON; lança <see cref="JsonException"/> se o texto não for um envelope.</summary>
    public static CashflowEvent<T>? Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<CashflowEvent<T>>(json, CashflowJson.Options);
}
