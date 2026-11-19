namespace Cashflow.Modules.Lancamentos.Models;

/// <summary>O desfecho de um registro ou estorno; o endpoint o traduz em status HTTP.</summary>
public abstract record RegistroResultado
{
    public sealed record Criado(LancamentoModel Lancamento) : RegistroResultado;

    public sealed record Idempotente(string RespostaJson) : RegistroResultado;

    public sealed record PayloadDivergente : RegistroResultado;

    public sealed record EmProcessamento : RegistroResultado;

    public sealed record NaoEncontrado : RegistroResultado;

    public sealed record RegraInvalida : RegistroResultado;

    public sealed record Conflito : RegistroResultado;
}
