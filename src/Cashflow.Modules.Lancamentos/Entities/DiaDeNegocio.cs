namespace Cashflow.Modules.Lancamentos.Entities;

/// <summary>
/// O dia em que um lançamento conta. O comerciante vive em São Paulo: uma venda às 23h30 de 17/11
/// (02h30 de 18/11 em UTC) é do dia 17, e é nesse consolidado que ela tem de aparecer.
/// </summary>
public static class DiaDeNegocio
{
    /// <summary>Converte o instante para o fuso do negócio e devolve a data local.</summary>
    public static DateOnly De(DateTimeOffset agora, TimeZoneInfo fuso) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(agora.UtcDateTime, fuso));
}
