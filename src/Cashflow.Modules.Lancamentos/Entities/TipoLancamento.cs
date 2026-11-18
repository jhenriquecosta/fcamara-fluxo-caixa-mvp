namespace Cashflow.Modules.Lancamentos.Entities;

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

    /// <summary>Só "debito" ou "credito": o <c>Enum.TryParse</c> aceitaria "0" e "1", fora do contrato.</summary>
    public static bool TryParseTexto(string? texto, out TipoLancamento tipo)
    {
        if (string.Equals(texto, "credito", StringComparison.OrdinalIgnoreCase))
        {
            tipo = TipoLancamento.Credito;
            return true;
        }

        tipo = TipoLancamento.Debito;
        return string.Equals(texto, "debito", StringComparison.OrdinalIgnoreCase);
    }
}
