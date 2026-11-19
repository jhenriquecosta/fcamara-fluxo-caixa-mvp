namespace Cashflow.Modules.Consolidado.Entities;

/// <summary>Um lançamento como o consolidado o enxerga: quanto ele muda o saldo de que dia.</summary>
/// <param name="EventId">O evento que trouxe o movimento; garante que ele seja somado uma vez só.</param>
/// <param name="ComercianteId">O dono do caixa.</param>
/// <param name="Tipo"><c>credito</c> ou <c>debito</c>, no texto do contrato.</param>
/// <param name="Valor">O valor, sempre positivo.</param>
/// <param name="Data">O dia de negócio em que o movimento conta.</param>
public sealed record MovimentoDoDia(Guid EventId, Guid ComercianteId, string Tipo, decimal Valor, DateOnly Data)
{
    /// <summary>Crédito soma; débito subtrai. O estorno já chega com o tipo oposto, então anula o original sozinho.</summary>
    public decimal EfeitoNoSaldo => Tipo == "credito" ? Valor : -Valor;
}
