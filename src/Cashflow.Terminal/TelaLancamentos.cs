using Cashflow.Blazor.Modules.Core;
using Cashflow.Blazor.Modules.Core.Services;
using Cashflow.Blazor.Modules.Lancamentos.Models;
using Spectre.Console;

namespace Cashflow.Terminal;

/// <summary>A tela dos lançamentos do dia, com a marca de estornado e a ação de estornar.</summary>
public sealed class TelaLancamentos(IAnsiConsole console, Sessao sessao)
{
    /// <summary>Lista o dia e, para o operador, oferece o estorno de um lançamento estornável.</summary>
    /// <param name="cancellationToken">Cancelamento.</param>
    public async Task ExecutarAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<LancamentoModel> itens;
        try
        {
            itens = await sessao.Lancamentos.ListarAsync(null, cancellationToken);
        }
        catch (ApiException ex)
        {
            console.WriteLine($"Falha ao listar os lançamentos: {ex.Message}");
            return;
        }

        if (itens.Count == 0)
        {
            console.WriteLine("Nenhum lançamento hoje.");
            return;
        }

        var tabela = new Table();
        tabela.AddColumn("Nº");
        tabela.AddColumn("Tipo");
        tabela.AddColumn("Valor");
        tabela.AddColumn("Descrição");
        tabela.AddColumn("Situação");
        for (var i = 0; i < itens.Count; i++)
        {
            var item = itens[i];
            tabela.AddRow(
                (i + 1).ToString(),
                Formatacao.Tipo(item.Tipo),
                Formatacao.Moeda(item.Valor),
                item.Descricao ?? "—",
                item.Estornado ? "estornado" : item.EhEstorno ? "estorno" : "—");
        }
        console.Write(tabela);

        if (!sessao.PodeLancar)
        {
            console.WriteLine("Perfil de consulta: somente leitura.");
            return;
        }

        var escolha = console.Prompt(new TextPrompt<int>("Estornar qual? (0 para voltar): "));
        if (escolha == 0)
            return;

        var alvo = escolha >= 1 && escolha <= itens.Count ? itens[escolha - 1] : null;
        if (alvo is null || !alvo.PodeEstornar)
        {
            console.WriteLine("Este lançamento não pode ser estornado.");
            return;
        }

        try
        {
            var estorno = await sessao.Lancamentos.EstornarAsync(
                alvo.Id, Guid.NewGuid().ToString(), cancellationToken);
            console.WriteLine($"Estorno {estorno.Id} criado para o lançamento {alvo.Id}.");
        }
        catch (ApiException ex)
        {
            console.WriteLine($"Falha ao estornar: {ex.Message}");
        }
    }
}
