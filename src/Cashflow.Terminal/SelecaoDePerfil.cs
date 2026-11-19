using Cashflow.Blazor.Modules.Core.Authorization;
using Spectre.Console;

namespace Cashflow.Terminal;

/// <summary>A escolha do perfil no início do terminal: operador lança e estorna, consulta só lê.</summary>
public static class SelecaoDePerfil
{
    /// <summary>Pergunta o perfil; Enter mantém o operador.</summary>
    /// <param name="console">O console onde a pergunta aparece.</param>
    public static string Escolher(IAnsiConsole console) =>
        console.Prompt(new TextPrompt<int>("Perfil — 1 operador · 2 consulta (padrão 1): ").DefaultValue(1)) == 2
            ? Perfis.Consulta
            : Perfis.Operador;
}
