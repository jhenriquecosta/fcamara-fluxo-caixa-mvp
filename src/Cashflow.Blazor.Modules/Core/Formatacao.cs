using System.Globalization;

namespace Cashflow.Blazor.Modules.Core;

public static class Formatacao
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static string Moeda(decimal valor) => valor.ToString("C", PtBr);

    public static string Dia(DateOnly dia) => dia.ToString("dd/MM/yyyy", PtBr);

    public static string Hora(DateTimeOffset instante) =>
        TimeZoneInfo.ConvertTime(instante, TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"))
            .ToString("HH:mm:ss", PtBr);

    public static string Tipo(string tipo) => tipo == "credito" ? "Crédito" : "Débito";
}
