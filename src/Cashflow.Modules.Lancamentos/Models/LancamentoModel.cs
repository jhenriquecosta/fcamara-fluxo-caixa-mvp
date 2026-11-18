using Cashflow.Modules.Lancamentos.Entities;

namespace Cashflow.Modules.Lancamentos.Models;

public sealed record LancamentoModel(
    Guid Id, Guid ComercianteId, DateOnly Data, decimal Valor,
    string Tipo, string? Descricao, Guid? EstornoDe)
{
    public static LancamentoModel De(Lancamento lancamento) =>
        new(lancamento.Id, lancamento.ComercianteId, lancamento.Data, lancamento.Valor,
            lancamento.Tipo.Texto(), lancamento.Descricao, lancamento.EstornoDe);
}
