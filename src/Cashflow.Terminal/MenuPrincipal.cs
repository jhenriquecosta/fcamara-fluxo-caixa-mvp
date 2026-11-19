using Spectre.Console;

namespace Cashflow.Terminal;

/// <summary>O menu principal do terminal: as ações do perfil da sessão, em laço até o Operador sair.</summary>
public sealed class MenuPrincipal(IAnsiConsole console, Sessao sessao)
{
    /// <summary>Executa o menu até a opção de sair.</summary>
    /// <param name="cancellationToken">Cancelamento.</param>
    public async Task ExecutarAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var opcoes = Opcoes();
            console.WriteLine();
            console.MarkupLine("[bold]Cashflow · terminal[/]");
            for (var i = 0; i < opcoes.Count; i++)
                console.WriteLine($"  {i + 1}. {opcoes[i].Titulo}");
            console.WriteLine("  0. Sair");

            var escolha = console.Prompt(new TextPrompt<int>("Escolha: "));
            if (escolha == 0)
                return;
            if (escolha < 1 || escolha > opcoes.Count)
            {
                console.WriteLine("Opção inválida.");
                continue;
            }

            await opcoes[escolha - 1].Executar(cancellationToken);
        }
    }

    private List<(string Titulo, Func<CancellationToken, Task> Executar)> Opcoes()
    {
        var opcoes = new List<(string, Func<CancellationToken, Task>)>
        {
            ("Saúde da infraestrutura", ct => new TelaSaude(console, sessao).ExecutarAsync(ct))
        };
        if (sessao.PodeLancar)
            opcoes.Add(("Lançar crédito/débito", ct => new TelaLancar(console, sessao).ExecutarAsync(ct)));
        opcoes.Add(("Lançamentos do dia", ct => new TelaLancamentos(console, sessao).ExecutarAsync(ct)));
        opcoes.Add(("Consolidado", ct => new TelaConsolidado(console, sessao).ExecutarAsync(ct)));
        opcoes.Add(("Métricas", ct => new TelaMetricas(console, sessao).ExecutarAsync(ct)));
        opcoes.Add(("Logs", ct => new TelaLogs(console, sessao).ExecutarAsync(ct)));
        opcoes.Add(("Demo ponta a ponta", ct => new TelaDemo(console, sessao).ExecutarAsync(ct)));
        return opcoes;
    }
}
