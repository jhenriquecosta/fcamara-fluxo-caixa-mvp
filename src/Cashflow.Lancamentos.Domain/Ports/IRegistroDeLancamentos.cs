namespace Cashflow.Lancamentos.Domain.Ports;

public enum EstornoResultado
{
    Criado,
    NaoEncontrado,
    RegraInvalida,
    Conflito
}

/// <summary>
/// Porta de escrita do agregado: a gravação do lançamento, do evento de outbox e
/// da resposta da chave de idempotência acontece numa única transação.
/// </summary>
public interface IRegistroDeLancamentos
{
    Task RegistrarAsync(
        Lancamento lancamento, string respostaJson, string chaveIdempotencia, CancellationToken ct);

    Task<(EstornoResultado resultado, Lancamento? estorno)> EstornarAsync(
        Guid comercianteId, Guid lancamentoId, DateOnly data, DateTimeOffset agora,
        string chaveIdempotencia, CancellationToken ct);
}
