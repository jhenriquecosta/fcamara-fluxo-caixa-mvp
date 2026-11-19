using Cashflow.Blazor.Modules.Core.Services;
using Spectre.Console;

namespace Cashflow.Terminal;

/// <summary>A tela de métricas: os sinais dourados que as regras de alerta vigiam.</summary>
public sealed class TelaMetricas(IAnsiConsole console, Sessao sessao)
{
    /// <summary>Consulta e mostra os sinais do Prometheus.</summary>
    /// <param name="cancellationToken">Cancelamento.</param>
    public async Task ExecutarAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var sinais = await sessao.Prometheus.ObterSinaisAsync(cancellationToken);
            var tabela = new Table();
            tabela.AddColumn("Serviço");
            tabela.AddColumn("Up");
            tabela.AddColumn("Req/s");
            tabela.AddColumn("P95");
            tabela.AddColumn("Erro");
            foreach (var servico in sinais.Servicos)
            {
                tabela.AddRow(
                    servico.Servico,
                    servico.Up ? "up" : "down",
                    servico.RequisicoesPorSegundo.ToString("F1"),
                    servico.P95Ms is { } p95 ? $"{p95:F0} ms" : "—",
                    servico.TaxaErro is { } taxa ? $"{taxa:P1}" : "—");
            }
            console.Write(tabela);
            console.WriteLine(
                $"Outbox: {Numero(sinais.OutboxIdadeSegundos)} s · Lag: {Numero(sinais.ConsumerLag)} · " +
                $"DLT (1h): {sinais.DltUltimaHora:F0}");
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            console.WriteLine($"Métricas indisponíveis: {ex.Message}");
        }
    }

    private static string Numero(double? valor) => valor is { } v ? v.ToString("F1") : "—";
}
