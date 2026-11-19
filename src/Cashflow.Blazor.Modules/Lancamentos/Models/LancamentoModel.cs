namespace Cashflow.Blazor.Modules.Lancamentos.Models;

/// <summary>O item de <c>GET /api/v1/lancamentos</c>: o contrato da API, não a entidade do módulo.</summary>
public sealed record LancamentoModel(
    Guid Id, DateOnly Data, decimal Valor, string Tipo, string? Descricao,
    Guid? EstornoDe, bool Estornado, DateTimeOffset CriadoEm)
{
    public bool EhEstorno => EstornoDe is not null;

    /// <summary>Um estorno não se estorna e um lançamento só se estorna uma vez (ADR-0004).</summary>
    public bool PodeEstornar => !Estornado && !EhEstorno;

    /// <summary>Crédito soma, débito subtrai: o efeito do lançamento no saldo do dia.</summary>
    public decimal Efeito => Tipo == "credito" ? Valor : -Valor;
}
