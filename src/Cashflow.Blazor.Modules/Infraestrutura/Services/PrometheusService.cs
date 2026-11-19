using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Cashflow.Blazor.Modules.Infraestrutura.Models;

namespace Cashflow.Blazor.Modules.Infraestrutura.Services;

/// <summary>
/// Lê os sinais pela API de consulta instantânea do Prometheus. As expressões são as das regras de
/// gravação e alerta do repositório, para o painel mostrar o que o alarme vigia.
/// </summary>
public class PrometheusService(HttpClient httpClient)
{
    private const string Negocio = "http_route!~\"/metrics|/health\"";

    /// <summary>Os serviços da aplicação, na ordem do painel.</summary>
    public static readonly string[] ServicosDaAplicacao = ["lancamentos", "consolidado", "background-server"];

    /// <summary>Uma consulta instantânea: cada série vira (rótulos, valor).</summary>
    public async Task<IReadOnlyList<(IReadOnlyDictionary<string, string> Rotulos, double Valor)>> ConsultarAsync(
        string expressao, CancellationToken cancellationToken = default)
    {
        var url = $"api/v1/query?query={Uri.EscapeDataString(expressao)}";
        var json = await httpClient.GetFromJsonAsync<JsonElement>(url, cancellationToken);
        if (json.GetProperty("status").GetString() != "success")
            throw new InvalidOperationException($"Prometheus recusou a consulta: {expressao}");

        var series = new List<(IReadOnlyDictionary<string, string>, double)>();
        foreach (var item in json.GetProperty("data").GetProperty("result").EnumerateArray())
        {
            var rotulos = item.GetProperty("metric").EnumerateObject()
                .ToDictionary(p => p.Name, p => p.Value.GetString() ?? string.Empty);
            var texto = item.GetProperty("value")[1].GetString();
            if (double.TryParse(texto, NumberStyles.Float, CultureInfo.InvariantCulture, out var valor) && double.IsFinite(valor))
                series.Add((rotulos, valor));
        }
        return series;
    }

    /// <summary>Todos os sinais do painel, com as consultas em paralelo.</summary>
    public async Task<SinaisModel> ObterSinaisAsync(CancellationToken cancellationToken = default)
    {
        var up = ConsultarAsync("up{job=~\"lancamentos|consolidado|background-server\"}", cancellationToken);
        var rps = ConsultarAsync($"sum by (service) (rate(http_server_request_duration_seconds_count{{{Negocio}}}[1m]))", cancellationToken);
        var p95 = ConsultarAsync($"histogram_quantile(0.95, sum by (service, le) (rate(http_server_request_duration_seconds_bucket{{{Negocio}}}[5m])))", cancellationToken);
        var erros = ConsultarAsync($"sum by (service) (rate(http_server_request_duration_seconds_count{{{Negocio},http_response_status_code=~\"5..\"}}[5m])) / sum by (service) (rate(http_server_request_duration_seconds_count{{{Negocio}}}[5m]))", cancellationToken);
        var outbox = ConsultarAsync("max(cashflow_outbox_pending_age_seconds)", cancellationToken);
        var lag = ConsultarAsync("max(cashflow_consumer_lag)", cancellationToken);
        var dlt = ConsultarAsync("sum(increase(cashflow_dlt_events_total[1h]))", cancellationToken);
        var lancamentos = ConsultarAsync("sum by (resultado) (cashflow_lancamentos_total)", cancellationToken);
        await Task.WhenAll(up, rps, p95, erros, outbox, lag, dlt, lancamentos);

        double? Por(IEnumerable<(IReadOnlyDictionary<string, string> Rotulos, double Valor)> series, string chave, string servico) =>
            series.Where(s => s.Rotulos.TryGetValue(chave, out var v) && v == servico).Select(s => (double?)s.Valor).FirstOrDefault();

        var servicos = ServicosDaAplicacao.Select(servico => new SinalServico(
            servico,
            Por(up.Result, "job", servico) == 1,
            Por(rps.Result, "service", servico) ?? 0,
            Por(p95.Result, "service", servico) * 1000,
            Por(erros.Result, "service", servico))).ToList();

        return new SinaisModel(
            servicos,
            outbox.Result.Select(s => (double?)s.Valor).FirstOrDefault(),
            lag.Result.Select(s => (double?)s.Valor).FirstOrDefault(),
            dlt.Result.Select(s => s.Valor).FirstOrDefault(), // sem série = nenhum evento em DLT
            lancamentos.Result.ToDictionary(s => s.Rotulos.GetValueOrDefault("resultado", "?"), s => s.Valor));
    }
}
