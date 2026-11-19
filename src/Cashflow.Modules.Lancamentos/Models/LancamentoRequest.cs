namespace Cashflow.Modules.Lancamentos.Models;

/// <summary>
/// O corpo do <c>POST /api/v1/lancamentos</c>. O tipo chega como texto para que o endpoint recuse
/// com 422 o que está fora do contrato, em vez de o desserializador aceitar números.
/// </summary>
public sealed record LancamentoRequest(decimal Valor, string? Tipo, string? Descricao);
