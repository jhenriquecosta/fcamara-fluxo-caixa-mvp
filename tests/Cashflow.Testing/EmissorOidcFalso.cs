using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Cashflow.Testing;

/// <summary>
/// Um emissor OIDC falso para o teste da estratégia de produção: serve o metadata e o JWKS com uma
/// chave RSA, e assina tokens com ela. O host valida pelo JWKS publicado — o caminho do Cognito,
/// sem AWS e sem rede externa.
/// </summary>
public sealed class EmissorOidcFalso : IAsyncDisposable
{
    private readonly RSA _rsa = RSA.Create(2048);
    private readonly WebApplication _app;

    private EmissorOidcFalso(WebApplication app) => _app = app;

    /// <summary>O endereço do emissor (o <c>Auth:Authority</c> do host).</summary>
    public string Authority { get; private set; } = string.Empty;

    /// <summary>O identificador da chave no JWKS.</summary>
    public string Kid { get; } = Guid.NewGuid().ToString("N");

    /// <summary>Sobe o emissor numa porta livre do loopback.</summary>
    public static async Task<EmissorOidcFalso> IniciarAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();
        var emissor = new EmissorOidcFalso(app);

        app.MapGet("/.well-known/openid-configuration", () => Results.Json(new
        {
            issuer = emissor.Authority,
            jwks_uri = $"{emissor.Authority}/jwks",
            token_endpoint = $"{emissor.Authority}/token",
            response_types_supported = new[] { "code" },
            subject_types_supported = new[] { "public" },
            id_token_signing_alg_values_supported = new[] { "RS256" }
        }));
        app.MapGet("/jwks", () => Results.Json(new { keys = new[] { emissor.Jwk() } }));

        await app.StartAsync();
        emissor.Authority = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.First();
        return emissor;
    }

    /// <summary>Um token do emissor, assinado pela chave publicada, com o comerciante e os escopos.</summary>
    public string CriarToken(Guid comercianteId, params string[] escopos) =>
        CriarToken(Chave(), comercianteId, escopos);

    /// <summary>Um token assinado por outra chave — o JWKS publicado não o valida.</summary>
    public string CriarTokenDeOutraChave(Guid comercianteId, params string[] escopos) =>
        CriarToken(new RsaSecurityKey(RSA.Create(2048)) { KeyId = "outra-chave" }, comercianteId, escopos);

    private RsaSecurityKey Chave() => new(_rsa) { KeyId = Kid };

    private string CriarToken(RsaSecurityKey chave, Guid comercianteId, string[] escopos)
    {
        var agora = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: Authority,
            audience: TestTokens.Audience,
            claims:
            [
                new Claim("sub", Guid.NewGuid().ToString()),
                new Claim("comercianteId", comercianteId.ToString()),
                new Claim("scope", string.Join(' ', escopos))
            ],
            notBefore: agora,
            expires: agora.AddHours(1),
            signingCredentials: new SigningCredentials(chave, SecurityAlgorithms.RsaSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private object Jwk()
    {
        var parametros = _rsa.ExportParameters(false);
        return new
        {
            kty = "RSA",
            use = "sig",
            alg = "RS256",
            kid = Kid,
            n = Base64Url(parametros.Modulus!),
            e = Base64Url(parametros.Exponent!)
        };
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
        _rsa.Dispose();
    }
}
