using System.Globalization;

namespace Cashflow.Blazor.Modules.Core;

/// <summary>Como valores aparecem na tela: real, data e hora de São Paulo, tipo por extenso.</summary>
public static class Formatacao
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>R$ no formato brasileiro.</summary>
    public static string Moeda(decimal valor) => valor.ToString("C", PtBr);

    /// <summary>dd/MM/yyyy.</summary>
    public static string Dia(DateOnly dia) => dia.ToString("dd/MM/yyyy", PtBr);

    /// <summary>A hora de São Paulo, não a do servidor.</summary>
    public static string Hora(DateTimeOffset instante) =>
        TimeZoneInfo.ConvertTime(instante, TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"))
            .ToString("HH:mm:ss", PtBr);

    /// <summary>"Crédito" ou "Débito", a partir do texto do contrato.</summary>
    public static string Tipo(string tipo) => tipo == "credito" ? "Crédito" : "Débito";
}
