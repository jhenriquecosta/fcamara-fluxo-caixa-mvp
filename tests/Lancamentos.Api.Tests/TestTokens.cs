using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace Cashflow.Lancamentos.Api.Tests;

public static class TestTokens
{
    public const string Issuer = "cashflow-tests";
    public const string Audience = "cashflow-tests";
    public const string SigningKey = "test-signing-key-with-at-least-32-bytes!!";

    public static string Create(Guid comercianteId, params string[] scopes)
    {
        var claims = new List<Claim>
        {
            new("sub", Guid.NewGuid().ToString()),
            new("comercianteId", comercianteId.ToString()),
            new("scope", string.Join(' ', scopes))
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
