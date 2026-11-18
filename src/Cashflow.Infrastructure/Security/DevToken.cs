using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Cashflow.Infrastructure.Security;

public sealed record DevTokenRequest(Guid ComercianteId, string[]? Scopes);

public static class DevToken
{
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
