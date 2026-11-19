using Cashflow.Blazor.Modules.Core;
using Cashflow.Blazor.Modules.Core.Services;
using Spectre.Console;

namespace Cashflow.Terminal;

/// <summary>A tela do consolidado diário: saldo do dia, anterior e acumulado.</summary>
public sealed class TelaConsolidado(IAnsiConsole console, Sessao sessao)
{
    /// <summary>Mostra o consolidado do dia de negócio; serviço fora aparece como indisponível.</summary>
    /// <param name="cancellationToken">Cancelamento.</param>
    public async Task ExecutarAsync(CancellationToken cancellationToken = default)
    {
        var hoje = new DiaDeNegocio(sessao.Relogio).Hoje();
        try
        {
            var consolidado = await sessao.Consolidado.ObterAsync(hoje, cancellationToken);
            var tabela = new Table();
            tabela.AddColumn("Dia");
            tabela.AddColumn("Saldo do dia");
            tabela.AddColumn("Saldo anterior");
            tabela.AddColumn("Saldo acumulado");
            tabela.AddRow(
                Formatacao.Dia(consolidado.Data),
                Formatacao.Moeda(consolidado.SaldoDia),
                Formatacao.Moeda(consolidado.SaldoAnterior),
                Formatacao.Moeda(consolidado.SaldoAcumulado));
            console.Write(tabela);
        }
        catch (ApiException ex)
        {
            console.WriteLine($"Consolidado indisponível: {ex.Message}");
        }
    }
}
