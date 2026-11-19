using Cashflow.Blazor.Modules.Core.Services;
using Spectre.Console;

namespace Cashflow.Terminal;

/// <summary>A tela de saúde: cada serviço da infraestrutura, no ar ou fora.</summary>
public sealed class TelaSaude(IAnsiConsole console, Sessao sessao)
{
    /// <summary>Confere e mostra a saúde de cada serviço configurado.</summary>
    /// <param name="cancellationToken">Cancelamento.</param>
    public async Task ExecutarAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var servicos = await sessao.Saude.ChecarAsync(cancellationToken);
            var tabela = new Table();
            tabela.AddColumn("Serviço");
            tabela.AddColumn("Papel");
            tabela.AddColumn("Situação");
            tabela.AddColumn("Latência");
            tabela.AddColumn("Detalhe");
            foreach (var servico in servicos)
            {
                tabela.AddRow(
                    servico.Nome,
                    servico.Papel,
                    servico.Disponivel ? "no ar" : "fora",
                    $"{servico.LatenciaMs:F0} ms",
                    servico.Erro ?? "—");
            }
            console.Write(tabela);
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            console.WriteLine($"Saúde indisponível: {ex.Message}");
        }
    }
}
