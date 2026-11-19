namespace Cashflow.CrossCuttingConcerns.DateTimes;

/// <summary>
/// O relógio do sistema atrás de uma interface. Nenhuma regra lê <c>DateTime.UtcNow</c> direto:
/// o teste fixa o instante e prova, por exemplo, que 23h30 em São Paulo ainda é o dia anterior em UTC+0.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>O instante atual em UTC, com offset. É o único relógio que o domínio consulta.</summary>
    DateTimeOffset OffsetUtcNow { get; }
}
