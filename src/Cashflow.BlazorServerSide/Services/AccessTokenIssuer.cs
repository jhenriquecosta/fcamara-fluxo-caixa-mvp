using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Cashflow.Blazor.Modules.Core.Authorization;
using Cashflow.Blazor.Modules.Core.Services;
using Cashflow.BlazorServerSide.ConfigurationOptions;
using Cashflow.Infrastructure.Security;
using Microsoft.IdentityModel.Tokens;

namespace Cashflow.BlazorServerSide.Services;

/// <summary>
/// Emite o access token que a API aceita, com só os escopos do perfil e vida curta (ADR-0010).
/// É a única peça que some quando o IdP real entrar.
/// </summary>
public sealed class AccessTokenIssuer(AppSettings settings, TimeProvider timeProvider)
{
    public static string[] Escopos(string perfil) => perfil switch
    {
        Perfis.Operador => [Scopes.LancamentosWrite, Scopes.LancamentosRead, Scopes.ConsolidadoRead],
        Perfis.Consulta => [Scopes.LancamentosRead, Scopes.ConsolidadoRead],
        _ => []
    };

    public TokenModel Emitir(ClaimsPrincipal usuario)
    {
        var perfil = usuario.FindFirst(Perfis.ClaimType)?.Value ?? string.Empty;
        var comercianteId = usuario.FindFirst("comercianteId")?.Value
                            ?? throw new InvalidOperationException("sessão sem comercianteId");

        var agora = timeProvider.GetUtcNow();
        var expira = agora.AddMinutes(settings.Auth.AccessTokenMinutes);
        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Auth.SigningKey));

        var token = new JwtSecurityToken(
            issuer: settings.Auth.Issuer,
            audience: settings.Auth.Audience,
            claims:
            [
                new Claim("sub", usuario.FindFirst("sub")?.Value ?? string.Empty),
                new Claim("comercianteId", comercianteId),
                new Claim("scope", string.Join(' ', Escopos(perfil)))
            ],
            notBefore: agora.UtcDateTime,
            expires: expira.UtcDateTime,
            signingCredentials: new SigningCredentials(chave, SecurityAlgorithms.HmacSha256));

        return new TokenModel(new JwtSecurityTokenHandler().WriteToken(token), expira);
    }
}
