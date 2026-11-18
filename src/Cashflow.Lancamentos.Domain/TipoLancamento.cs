namespace Cashflow.Lancamentos.Domain;

public enum TipoLancamento
{
    Debito,
    Credito
}

public static class TipoLancamentoExtensions
{
    public static string Texto(this TipoLancamento tipo) =>
        tipo == TipoLancamento.Credito ? "credito" : "debito";

    public static TipoLancamento Oposto(this TipoLancamento tipo) =>
        tipo == TipoLancamento.Credito ? TipoLancamento.Debito : TipoLancamento.Credito;

    public static bool TryParseTexto(string? texto, out TipoLancamento tipo) =>
        Enum.TryParse(texto ?? string.Empty, ignoreCase: true, out tipo) && Enum.IsDefined(tipo);
}
