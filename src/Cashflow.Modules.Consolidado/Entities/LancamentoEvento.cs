namespace Cashflow.Modules.Consolidado.Entities;

public sealed record LancamentoEvento(
    Guid EventId, Guid ComercianteId, string Tipo, decimal Valor, DateOnly Data)
{
    /// <summary>Crédito soma; débito subtrai. O estorno chega como o tipo oposto.</summary>
    public decimal Delta => Tipo == "credito" ? Valor : -Valor;
}
