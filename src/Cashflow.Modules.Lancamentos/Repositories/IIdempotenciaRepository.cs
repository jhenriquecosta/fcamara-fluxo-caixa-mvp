namespace Cashflow.Modules.Lancamentos.Repositories;

/// <summary>Em que pé está uma chave de idempotência quando a requisição chega.</summary>
public enum SituacaoDaChave
{
    /// <summary>A chave é desta requisição agora: pode executar.</summary>
    Reservada,

    /// <summary>Já foi concluída com o mesmo pedido: devolve a resposta gravada.</summary>
    JaConcluida,

    /// <summary>Já foi usada com outro pedido: recusa.</summary>
    UsadaComOutroPedido,

    /// <summary>Outra requisição a reservou há menos de 30 s e não terminou: recusa, o cliente tenta de novo.</summary>
    EmProcessamento
}

/// <summary>A resposta da reserva: a situação da chave e, se já concluída, a resposta gravada.</summary>
public sealed record ReservaDaChave(SituacaoDaChave Situacao, string? RespostaGravada = null);

/// <summary>
/// A chave de idempotência por comerciante (ADR-0004). A mesma requisição repetida, por timeout ou
/// retry do cliente, não grava dois lançamentos: devolve a resposta da primeira.
/// </summary>
/// <remarks>
/// A reserva é gravada fora da transação do lançamento, para que uma requisição concorrente com a mesma
/// chave a enxergue já em processamento. Uma reserva presa há mais de 30 s (o processo caiu no meio) pode ser retomada.
/// </remarks>
public interface IIdempotenciaRepository
{
    /// <summary>
    /// Tenta tomar a chave para esta requisição. <paramref name="impressaoDoPedido"/> identifica o pedido
    /// (o corpo do registro, ou o lançamento alvo do estorno) e é o que distingue repetição de uso indevido.
    /// </summary>
    Task<ReservaDaChave> ReservarAsync(Guid comercianteId, string chave, string impressaoDoPedido,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fecha a chave com o lançamento gravado e a resposta a repetir. Roda dentro da transação do
    /// lançamento: ou os dois ficam, ou nenhum.
    /// </summary>
    Task ConcluirAsync(Guid comercianteId, string chave, Guid lancamentoId, string respostaJson,
        CancellationToken cancellationToken = default);

    /// <summary>Marca a chave como falha, liberando-a para uma nova tentativa do cliente com o mesmo pedido.</summary>
    Task MarcarFalhaAsync(Guid comercianteId, string chave, CancellationToken cancellationToken = default);
}
