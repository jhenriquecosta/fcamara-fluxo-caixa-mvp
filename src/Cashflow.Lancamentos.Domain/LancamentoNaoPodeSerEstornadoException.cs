namespace Cashflow.Lancamentos.Domain;

public sealed class LancamentoNaoPodeSerEstornadoException()
    : Exception("um estorno não pode ser estornado");
