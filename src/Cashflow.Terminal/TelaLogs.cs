using Cashflow.Blazor.Modules.Core;
using Cashflow.Blazor.Modules.Core.Services;
using Spectre.Console;

namespace Cashflow.Terminal;

/// <summary>A tela de logs: as últimas entradas do Elasticsearch.</summary>
public sealed class TelaLogs(IAnsiConsole console, Sessao sessao)
{
    /// <summary>Consulta e mostra as últimas entradas de log.</summary>
    /// <param name="cancellationToken">Cancelamento.</param>
    public async Task ExecutarAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var entradas = await sessao.Logs.UltimosAsync(20, null, null, cancellationToken);
            if (entradas.Count == 0)
            {
                console.WriteLine("Nenhum log encontrado.");
                return;
            }

            var tabela = new Table();
            tabela.AddColumn("Hora");
            tabela.AddColumn("Nível");
            tabela.AddColumn("Serviço");
            tabela.AddColumn("Mensagem");
            foreach (var entrada in entradas)
                tabela.AddRow(Formatacao.Hora(entrada.Instante), entrada.Nivel, entrada.Servico, entrada.Mensagem);
            console.Write(tabela);
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            console.WriteLine($"Logs indisponíveis: {ex.Message}");
        }
    }
}
