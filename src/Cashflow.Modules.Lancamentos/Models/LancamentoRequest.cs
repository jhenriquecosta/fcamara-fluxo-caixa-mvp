namespace Cashflow.Modules.Lancamentos.Models;

public sealed record LancamentoRequest(decimal Valor, string? Tipo, string? Descricao);
