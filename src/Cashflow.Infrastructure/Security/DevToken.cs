using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Cashflow.Infrastructure.Security;

/// <summary>O pedido de token do ambiente de desenvolvimento: o comerciante e, opcionalmente, os escopos.</summary>
public sealed record DevTokenRequest(Guid ComercianteId, string[]? Scopes);

/// <summary>
/// Emite um JWT local para o demo e os testes manuais. Só existe em <c>Development</c>: em produção quem
/// emite o token é o provedor de identidade, e a API apenas o valida.
/// </summary>
public static class DevToken
{
    /// <summary>Um token de 8 horas com o <c>comercianteId</c> e os escopos, assinado com a chave configurada.</summary>
    public static string Create(IConfiguration configuration, Guid comercianteId, string[] scopes)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Auth:SigningKey"]
                ?? throw new InvalidOperationException("Auth:SigningKey não configurada")));

        var claims = new List<Claim>
        {
            new("sub", Guid.NewGuid().ToString()),
            new("comercianteId", comercianteId.ToString()),
            new("scope", string.Join(' ', scopes))
        };

        var token = new JwtSecurityToken(
            issuer: configuration["Auth:Issuer"],
            audience: configuration["Auth:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
