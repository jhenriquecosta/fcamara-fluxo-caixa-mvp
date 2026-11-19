namespace Cashflow.Modules.Lancamentos.Entities;

public static class DiaDeNegocio
{
    public static DateOnly De(DateTimeOffset agora, TimeZoneInfo fuso) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(agora.UtcDateTime, fuso));
}
