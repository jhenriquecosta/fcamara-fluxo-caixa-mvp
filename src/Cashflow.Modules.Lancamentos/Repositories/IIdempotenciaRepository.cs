namespace Cashflow.Modules.Lancamentos.Repositories;

public enum ReivindicacaoTipo
{
    Reivindicado,
    Replay,
    PayloadDivergente,
    EmProcessamento
}

public sealed record Reivindicacao(ReivindicacaoTipo Tipo, string? RespostaJson = null);

/// <summary>
/// A chave de idempotência (ADR-0004). A reivindicação é gravada fora da transação do lançamento,
/// para que uma requisição concorrente com a mesma chave a enxergue já em <c>processando</c>.
/// </summary>
public interface IIdempotenciaRepository
{
    Task<Reivindicacao> ReivindicarAsync(Guid comercianteId, string chave, string hash, CancellationToken cancellationToken = default);

    Task ConcluirAsync(Guid comercianteId, string chave, Guid lancamentoId, string respostaJson,
        CancellationToken cancellationToken = default);

    Task FalharAsync(Guid comercianteId, string chave, CancellationToken cancellationToken = default);
}
