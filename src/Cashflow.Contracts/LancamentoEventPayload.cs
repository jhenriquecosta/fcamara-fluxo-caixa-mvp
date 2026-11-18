namespace Cashflow.Contracts;

public sealed record LancamentoEventPayload(
    Guid LancamentoId,
    Guid ComercianteId,
    string Tipo,
    decimal Valor,
    DateOnly Data,
    Guid? EstornoDe);
