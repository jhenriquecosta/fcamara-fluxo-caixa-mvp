using Cashflow.Lancamentos.Domain;

namespace Cashflow.Lancamentos.Application;

public sealed record RegistrarLancamentoCommand(
    Guid ComercianteId, TipoLancamento Tipo, decimal Valor, string? Descricao, string IdempotencyKey);

public sealed record EstornarLancamentoCommand(
    Guid ComercianteId, Guid LancamentoId, string IdempotencyKey);

public sealed record LancamentoResponse(
    Guid Id, Guid ComercianteId, DateOnly Data, decimal Valor,
    string Tipo, string? Descricao, Guid? EstornoDe);

public abstract record RegistroResultado
{
    public sealed record Criado(LancamentoResponse Resposta) : RegistroResultado;
    public sealed record Idempotente(string RespostaJson) : RegistroResultado;
    public sealed record PayloadDivergente : RegistroResultado;
    public sealed record EmProcessamento : RegistroResultado;
    public sealed record NaoEncontrado : RegistroResultado;
    public sealed record RegraInvalida : RegistroResultado;
    public sealed record Conflito : RegistroResultado;
}
