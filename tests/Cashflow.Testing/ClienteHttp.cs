using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Cashflow.Testing;

/// <summary>Atalhos de <see cref="HttpClient"/> para falar com as APIs como um comerciante.</summary>
public static class ClienteHttp
{
    /// <summary>Põe o token do comerciante com o escopo pedido e, se informada, a <c>Idempotency-Key</c>.</summary>
    public static HttpClient Autenticado(this HttpClient client, Guid comerciante, string escopo, string? chave = null)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", TestTokens.Create(comerciante, escopo));
        if (chave is not null)
            client.DefaultRequestHeaders.Add("Idempotency-Key", chave);
        return client;
    }

    /// <summary>Consulta o consolidado do dia até a condição valer: a projeção é eventualmente consistente.</summary>
    public static async Task<JsonElement?> EsperarConsolidadoAsync(
        this HttpClient cliente, string dia, Func<JsonElement, bool> condicao, TimeSpan timeout)
    {
        var ct = TestContext.Current.CancellationToken;
        var limite = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < limite)
        {
            var resposta = await cliente.GetAsync($"/api/v1/consolidado/{dia}", ct);
            if (resposta.StatusCode == HttpStatusCode.OK)
            {
                var json = await resposta.Content.ReadFromJsonAsync<JsonElement>(ct);
                if (condicao(json))
                    return json;
            }
            await Task.Delay(500, ct);
        }
        return null;
    }
}
