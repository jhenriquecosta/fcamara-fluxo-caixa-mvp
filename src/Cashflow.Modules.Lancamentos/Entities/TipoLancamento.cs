namespace Cashflow.Modules.Lancamentos.Entities;

/// <summary>A direção do dinheiro no caixa.</summary>
public enum TipoLancamento
{
    /// <summary>Sai do caixa: subtrai do saldo.</summary>
    Debito,

    /// <summary>Entra no caixa: soma ao saldo.</summary>
    Credito
}

/// <summary>A tradução do <see cref="TipoLancamento"/> de e para o contrato (API, banco e barramento falam <c>debito</c>/<c>credito</c>).</summary>
public static class TipoLancamentoExtensions
{
    /// <summary>O texto do contrato: <c>credito</c> ou <c>debito</c>.</summary>
    public static string NoContrato(this TipoLancamento tipo) =>
        tipo == TipoLancamento.Credito ? "credito" : "debito";

    /// <summary>O tipo que anula este no saldo: é o tipo do estorno.</summary>
    public static TipoLancamento Oposto(this TipoLancamento tipo) =>
        tipo == TipoLancamento.Credito ? TipoLancamento.Debito : TipoLancamento.Credito;

    /// <summary>
    /// Lê o texto do contrato, sem diferenciar maiúsculas. Só aceita <c>debito</c> ou <c>credito</c>:
    /// o <c>Enum.TryParse</c> aceitaria <c>"0"</c> e <c>"1"</c>, que estão fora do contrato (ADR-0009).
    /// </summary>
    public static bool TryLerDoContrato(string? texto, out TipoLancamento tipo)
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
