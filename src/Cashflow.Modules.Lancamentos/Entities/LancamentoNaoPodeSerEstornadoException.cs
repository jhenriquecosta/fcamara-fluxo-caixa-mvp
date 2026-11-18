namespace Cashflow.Modules.Lancamentos.Entities;

public sealed class LancamentoNaoPodeSerEstornadoException()
    : Exception("um estorno não pode ser estornado");
