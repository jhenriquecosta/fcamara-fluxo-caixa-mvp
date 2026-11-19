using Cashflow.Contracts;

namespace Cashflow.Modules.Lancamentos.Constants;

/// <summary>
/// Os tipos de evento que este módulo publica, como o <c>EventTypeConstants</c> da referência. Apontam
/// para o Contracts, que é a fonte: o módulo não inventa nome de evento que o consumidor não conheça.
/// </summary>
public static class EventTypeConstants
{
    /// <inheritdoc cref="EventTypes.LancamentoRegistrado"/>
    public const string LancamentoRegistrado = EventTypes.LancamentoRegistrado;

    /// <inheritdoc cref="EventTypes.LancamentoEstornado"/>
    public const string LancamentoEstornado = EventTypes.LancamentoEstornado;
}
