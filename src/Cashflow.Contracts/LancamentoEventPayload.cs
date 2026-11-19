namespace Cashflow.Contracts;

/// <summary>
/// O lançamento como o barramento o transporta. Basta para o consolidado projetar o saldo:
/// quem, quando, quanto e em que direção.
/// </summary>
/// <param name="LancamentoId">O lançamento (ou o estorno) que gerou o evento.</param>
/// <param name="ComercianteId">O dono do caixa; também a chave de partição.</param>
/// <param name="Tipo"><c>credito</c> ou <c>debito</c>. O estorno já chega com o tipo oposto ao original.</param>
/// <param name="Valor">Sempre positivo; o sinal vem do <paramref name="Tipo"/>.</param>
/// <param name="Data">O dia de negócio em America/Sao_Paulo.</param>
/// <param name="EstornoDe">O lançamento estornado, quando este é um estorno.</param>
public sealed record LancamentoEventPayload(
    Guid LancamentoId,
    Guid ComercianteId,
    string Tipo,
    decimal Valor,
    DateOnly Data,
    Guid? EstornoDe);
