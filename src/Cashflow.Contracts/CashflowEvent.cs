namespace Cashflow.Contracts;

/// <summary>
/// O envelope de todo evento publicado no barramento. O schema é o contrato entre os módulos:
/// mudou aqui, muda nos dois lados e ganha versão.
/// </summary>
/// <param name="EventId">Identidade única do evento. O consolidado descarta o que já viu por ela (at-least-once).</param>
/// <param name="EventType">O que aconteceu, um dos <see cref="EventTypes"/>.</param>
/// <param name="OccurredAt">Quando aconteceu, em UTC.</param>
/// <param name="Version">A versão do schema do <paramref name="Payload"/>.</param>
/// <param name="Payload">Os dados do fato.</param>
public sealed record CashflowEvent<T>(
    Guid EventId,
    string EventType,
    DateTimeOffset OccurredAt,
    int Version,
    T Payload);
