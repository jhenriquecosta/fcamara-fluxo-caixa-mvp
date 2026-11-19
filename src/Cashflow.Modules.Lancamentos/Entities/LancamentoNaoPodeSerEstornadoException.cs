namespace Cashflow.Modules.Lancamentos.Entities;

/// <summary>
/// Pediu-se o estorno de um estorno. A regra impede a cadeia de correções: para desfazer um estorno,
/// registra-se um lançamento novo.
/// </summary>
public sealed class LancamentoNaoPodeSerEstornadoException()
    : Exception("um estorno não pode ser estornado");
