using Cashflow.Modules.Lancamentos.Entities;

namespace Cashflow.Modules.Lancamentos.Models;

/// <summary>O lançamento como a API o devolve e como a resposta idempotente o guarda.</summary>
public sealed record LancamentoModel(
    Guid Id, Guid ComercianteId, DateOnly Data, decimal Valor,
    string Tipo, string? Descricao, Guid? EstornoDe)
{
    /// <summary>Projeta a entidade no modelo da API, com o tipo no texto do contrato.</summary>
    public static LancamentoModel De(Lancamento lancamento) =>
        new(lancamento.Id, lancamento.ComercianteId, lancamento.Data, lancamento.Valor,
            lancamento.Tipo.NoContrato(), lancamento.Descricao, lancamento.EstornoDe);
}
