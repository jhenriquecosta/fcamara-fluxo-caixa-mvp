using System.Net.Http.Json;
using System.Text.Json;
using Cashflow.Blazor.Modules.Infraestrutura.Models;

namespace Cashflow.Blazor.Modules.Infraestrutura.Services;

/// <summary>
/// Os últimos logs dos serviços, direto do índice <c>cashflow-logs-*</c> que o Serilog alimenta.
/// Os acessos de <c>/metrics</c> e <c>/health</c> ficam de fora: são o scrape e a checagem, não o negócio.
/// </summary>
public class LogsService(HttpClient httpClient)
{
    private static readonly string[] Ruido = ["/metrics", "/health"];

    /// <summary>Os níveis em ordem crescente de gravidade.</summary>
    public static readonly string[] Niveis = ["Information", "Warning", "Error", "Fatal"];

    /// <summary>As últimas entradas, da mais recente para a mais antiga.</summary>
    /// <param name="quantidade">Quantas linhas.</param>
    /// <param name="servico">Só deste serviço; nulo para todos.</param>
    /// <param name="nivelMinimo">Só deste nível para cima; nulo para todos.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    public async Task<IReadOnlyList<LogEntrada>> UltimosAsync(
        int quantidade = 30, string? servico = null, string? nivelMinimo = null,
        CancellationToken cancellationToken = default)
    {
        var filtros = new List<object>();
        if (!string.IsNullOrWhiteSpace(servico))
            filtros.Add(new { match_phrase = new Dictionary<string, string> { ["fields.service"] = servico } });
        if (nivelMinimo is not null && Array.IndexOf(Niveis, nivelMinimo) is var i and >= 0)
            filtros.Add(new { terms = new Dictionary<string, string[]> { ["level"] = Niveis[i..] } });

        var consulta = new
        {
            size = quantidade,
            sort = new object[] { new Dictionary<string, string> { ["@timestamp"] = "desc" } },
            query = new
            {
                @bool = new
                {
                    filter = filtros,
                    must_not = Ruido.Select(r => (object)new { match_phrase = new Dictionary<string, string> { ["fields.RequestPath"] = r } }).ToArray()
                }
            }
        };

        using var resposta = await httpClient.PostAsJsonAsync(
            "cashflow-logs-*/_search?ignore_unavailable=true&allow_no_indices=true", consulta, cancellationToken);
        resposta.EnsureSuccessStatusCode();
        var json = await resposta.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        return json.GetProperty("hits").GetProperty("hits").EnumerateArray()
            .Select(h => h.GetProperty("_source"))
            .Select(s => new LogEntrada(
                s.TryGetProperty("@timestamp", out var t) && t.TryGetDateTimeOffset(out var instante) ? instante : DateTimeOffset.MinValue,
                Texto(s, "level") ?? "?",
                s.TryGetProperty("fields", out var f) ? Texto(f, "service") ?? "?" : "?",
                Texto(s, "message") ?? string.Empty,
                s.TryGetProperty("fields", out var f2) ? Texto(f2, "SourceContext") : null))
            .ToList();
    }

    private static string? Texto(JsonElement elemento, string propriedade) =>
        elemento.TryGetProperty(propriedade, out var valor) && valor.ValueKind == JsonValueKind.String
            ? valor.GetString()
            : null;
}
