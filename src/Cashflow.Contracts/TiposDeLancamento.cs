namespace Cashflow.Contracts;

/// <summary>
/// O texto do tipo no evento de lançamento. É contrato: o Lançamentos escreve, o Consolidado lê, e um
/// terceiro valor ou outra grafia precisa quebrar alto dos dois lados, não virar débito em silêncio.
/// </summary>
public static class TiposDeLancamento
{
    /// <summary>Entrada de dinheiro: soma ao saldo.</summary>
    public const string Credito = "credito";

    /// <summary>Saída de dinheiro: subtrai do saldo.</summary>
    public const string Debito = "debito";
}
