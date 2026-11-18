using Cashflow.Modules.Lancamentos.Entities;

namespace Cashflow.Modules.Lancamentos.Repositories;

public enum ReivindicacaoTipo
{
    Reivindicado,
    Replay,
    PayloadDivergente,
    EmProcessamento
}

public sealed record Reivindicacao(ReivindicacaoTipo Tipo, string? RespostaJson = null);

public interface IPortDeIdempotencia
{
    Task<Reivindicacao> ReivindicarAsync(
        Guid comercianteId, string chave, string hash, CancellationToken ct);

    Task FalharAsync(Guid comercianteId, string chave, CancellationToken ct);
}
