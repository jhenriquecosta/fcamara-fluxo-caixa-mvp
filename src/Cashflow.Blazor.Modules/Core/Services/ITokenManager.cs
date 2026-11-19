namespace Cashflow.Blazor.Modules.Core.Services;

/// <summary>
/// De onde vem o access token que os serviços enviam à API. O host decide como obtê-lo
/// (ADR-0010): hoje emite localmente; com OIDC, devolve o token da sessão. As telas não mudam.
/// </summary>
public interface ITokenManager
{
    Task<TokenModel> GetTokenAsync(CancellationToken cancellationToken = default);
}

public sealed record TokenModel(string AccessToken, DateTimeOffset ExpiresAt)
{
    /// <summary>Vence com folga de 30 s, para não chegar vencido à API.</summary>
    public bool ExpiraAntesDe(DateTimeOffset agora) => ExpiresAt <= agora.AddSeconds(30);
}
