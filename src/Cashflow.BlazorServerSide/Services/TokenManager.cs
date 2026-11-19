using Cashflow.Blazor.Modules.Core.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace Cashflow.BlazorServerSide.Services;

/// <summary>
/// O <c>ITokenManager</c> do host, com escopo de circuito. O token vive só aqui, no servidor; o
/// navegador tem apenas o cookie de sessão. Difere da referência, que guarda tokens no
/// <c>ProtectedLocalStorage</c> do navegador.
/// </summary>
public sealed class TokenManager(
    AuthenticationStateProvider authenticationStateProvider,
    AccessTokenIssuer issuer,
    TimeProvider timeProvider) : ITokenManager
{
    private TokenModel? _atual;

    /// <inheritdoc />
    public async Task<TokenModel> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_atual is { } token && !token.ExpiraAntesDe(timeProvider.GetUtcNow()))
            return token;

        var state = await authenticationStateProvider.GetAuthenticationStateAsync();
        if (state.User.Identity?.IsAuthenticated != true)
            throw new ApiException(System.Net.HttpStatusCode.Unauthorized, "sessão expirada");

        return _atual = issuer.Emitir(state.User);
    }
}
