namespace Cashflow.Blazor.Modules.Core.Services;

/// <summary>O "hoje" da tela é o de São Paulo, o mesmo dia de negócio que a API usa.</summary>
public sealed class DiaDeNegocio(TimeProvider timeProvider)
{
    private static readonly TimeZoneInfo Fuso = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    /// <summary>A data de hoje em America/Sao_Paulo.</summary>
    public DateOnly Hoje() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(timeProvider.GetUtcNow().UtcDateTime, Fuso));
}
