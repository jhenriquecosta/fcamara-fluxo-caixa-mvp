namespace Cashflow.Contracts;

/// <summary>
/// Envelope canônico do evento publicado no barramento. O schema é o contrato
/// entre os serviços: mudou aqui, muda nos dois lados e ganha versão.
/// </summary>
public sealed record CashflowEvent<T>(
    Guid EventId,
    string EventType,
    DateTimeOffset OccurredAt,
    int Version,
    T Payload);
