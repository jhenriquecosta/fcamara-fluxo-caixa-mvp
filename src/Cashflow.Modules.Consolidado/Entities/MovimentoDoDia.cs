namespace Cashflow.Modules.Consolidado.Entities;

/// <summary>Um lançamento como o consolidado o enxerga: o quanto ele move o saldo do dia.</summary>
public sealed record MovimentoDoDia(Guid EventId, Guid ComercianteId, string Tipo, decimal Valor, DateOnly Data)
{
    /// <summary>Crédito soma; débito subtrai. O estorno chega como o tipo oposto.</summary>
    public decimal Delta => Tipo == "credito" ? Valor : -Valor;
}
