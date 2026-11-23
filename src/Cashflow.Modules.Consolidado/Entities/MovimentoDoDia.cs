using Cashflow.Contracts;

namespace Cashflow.Modules.Consolidado.Entities;

/// <summary>Um lançamento como o consolidado o enxerga: quanto ele muda o saldo de que dia.</summary>
/// <param name="EventId">O evento que trouxe o movimento; garante que ele seja somado uma vez só.</param>
/// <param name="ComercianteId">O dono do caixa.</param>
/// <param name="Tipo"><c>credito</c> ou <c>debito</c>, no texto do contrato.</param>
/// <param name="Valor">O valor, sempre positivo.</param>
/// <param name="Data">O dia de negócio em que o movimento conta.</param>
/// <exception cref="ArgumentException">
/// O tipo não é <c>credito</c> nem <c>debito</c>. Tratá-lo como débito somaria um valor errado em silêncio;
/// recusar faz o consumidor esgotar as tentativas e desviar o evento para a DLT, onde alguém olha.
/// </exception>
public sealed record MovimentoDoDia(Guid EventId, Guid ComercianteId, string Tipo, decimal Valor, DateOnly Data)
{
    /// <summary><c>credito</c> ou <c>debito</c>; qualquer outro texto é recusado na construção.</summary>
    public string Tipo { get; } = Tipo is TiposDeLancamento.Credito or TiposDeLancamento.Debito
        ? Tipo
        : throw new ArgumentException($"tipo de lançamento fora do contrato: '{Tipo}'", nameof(Tipo));

    /// <summary>Crédito soma; débito subtrai. O estorno já chega com o tipo oposto, então anula o original sozinho.</summary>
    public decimal EfeitoNoSaldo => Tipo == TiposDeLancamento.Credito ? Valor : -Valor;
}
