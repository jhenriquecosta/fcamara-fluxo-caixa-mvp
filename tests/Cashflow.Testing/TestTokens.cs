using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Cashflow.Testing;

/// <summary>
/// Emite JWTs com a chave dos testes. Os hosts sob teste recebem a mesma chave pelo <c>HostSettings</c>,
/// então o token é validado de verdade, como em produção.
/// </summary>
public static class TestTokens
{
    public const string Issuer = "cashflow-tests";
    public const string Audience = "cashflow-tests";
    public const string SigningKey = "test-signing-key-with-at-least-32-bytes!!";

    /// <summary>Um token de 1 hora do comerciante com os escopos dados.</summary>
    public static string Create(Guid comercianteId, params string[] scopes)
    {
        var claims = new List<Claim>
        {
            new("sub", Guid.NewGuid().ToString()),
            new("comercianteId", comercianteId.ToString()),
            new("scope", string.Join(' ', scopes))
        };

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)), SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
