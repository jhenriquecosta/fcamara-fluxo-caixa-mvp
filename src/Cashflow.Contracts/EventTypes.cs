namespace Cashflow.Contracts;

/// <summary>Os fatos que o módulo de Lançamentos publica.</summary>
public static class EventTypes
{
    /// <summary>Um crédito ou débito novo foi registrado.</summary>
    public const string LancamentoRegistrado = "lancamento.registrado";

    /// <summary>Um lançamento foi corrigido por um contra-lançamento de tipo oposto.</summary>
    public const string LancamentoEstornado = "lancamento.estornado";
}
