using System.Text.Json;

namespace Cashflow.Contracts;

public static class CashflowJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
