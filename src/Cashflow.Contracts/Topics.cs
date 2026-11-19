namespace Cashflow.Contracts;

/// <summary>Os nomes no Kafka. A versão no nome do tópico permite um <c>v2</c> conviver com o <c>v1</c> numa migração.</summary>
public static class Topics
{
    /// <summary>Os eventos de lançamento, particionados por comerciante.</summary>
    public const string Lancamentos = "cashflow.lancamentos.v1";

    /// <summary>A dead-letter: o evento que falhou em todas as tentativas, guardado para análise e reprocesso.</summary>
    public const string LancamentosDlt = "cashflow.lancamentos.v1.dlt";

    /// <summary>O consumer group do consolidado: as réplicas dividem as partições entre si.</summary>
    public const string GrupoConsolidado = "consolidado";
}
