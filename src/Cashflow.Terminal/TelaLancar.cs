using Cashflow.Blazor.Modules.Core.Services;
using Cashflow.Blazor.Modules.Lancamentos.Models;
using Spectre.Console;

namespace Cashflow.Terminal;

/// <summary>A tela de lançar: pergunta tipo, valor e descrição, e registra pela API.</summary>
public sealed class TelaLancar(IAnsiConsole console, Sessao sessao)
{
    /// <summary>Executa o formulário de lançamento.</summary>
    /// <param name="cancellationToken">Cancelamento.</param>
    public async Task ExecutarAsync(CancellationToken cancellationToken = default)
    {
        if (!sessao.PodeLancar)
        {
            console.WriteLine("Seu perfil é de consulta — sem permissão para lançar.");
            return;
        }

        var tipo = console.Prompt(new TextPrompt<string>("Tipo (credito/debito): ").DefaultValue("credito"));
        var valor = console.Prompt(new TextPrompt<decimal>("Valor: "));
        var descricao = console.Prompt(new TextPrompt<string>("Descrição (opcional): ").AllowEmpty());

        try
        {
            var criado = await sessao.Lancamentos.LancarAsync(
                new NovoLancamentoModel
                {
                    Tipo = tipo.Trim(),
                    Valor = valor,
                    Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim()
                },
                Guid.NewGuid().ToString(), cancellationToken);
            console.WriteLine($"Lançamento {criado.Id} registrado.");
        }
        catch (ApiException ex)
        {
            console.WriteLine($"Falha ao lançar: {ex.Message}");
        }
    }
}
