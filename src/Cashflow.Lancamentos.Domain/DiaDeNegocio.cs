namespace Cashflow.Lancamentos.Domain;

public static class DiaDeNegocio
{
    public static DateOnly De(DateTimeOffset agora, TimeZoneInfo fuso) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(agora.UtcDateTime, fuso));
}
