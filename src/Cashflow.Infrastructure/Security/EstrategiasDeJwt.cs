using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace Cashflow.Infrastructure.Security;

/// <summary>
/// As estratégias de validação do JWT (ADR-0012, item 2). O host escolhe uma por configuração:
/// <c>OidcAuthority</c> quando <c>Auth:Authority</c> existe (o alvo Cognito), e
/// <c>ChaveDeDesenvolvimento</c> no local. O handler continua sendo o do framework.
/// </summary>
internal abstract class EstrategiaDeJwt : IConfigureNamedOptions<JwtBearerOptions>
{
    /// <inheritdoc />
    public void Configure(string? name, JwtBearerOptions options)
    {
        if (name != JwtBearerDefaults.AuthenticationScheme)
            return;

        options.MapInboundClaims = false;
        options.Events = Eventos();
        Aplicar(options);
    }

    /// <inheritdoc />
    public void Configure(JwtBearerOptions options) => Configure(JwtBearerDefaults.AuthenticationScheme, options);

    /// <summary>O que distingue uma estratégia da outra: de onde vêm emissor, audiência e chave.</summary>
    protected abstract void Aplicar(JwtBearerOptions options);

    private static JwtBearerEvents Eventos() => new()
    {
        OnAuthenticationFailed = context =>
        {
            Log.Warning("autenticação falhou: {Reason}", context.Exception.Message);
            return Task.CompletedTask;
        },
        OnForbidden = context =>
        {
            Log.Warning("autorização negada para {Path}", context.Request.Path);
            return Task.CompletedTask;
        }
    };
}

/// <summary>
/// A estratégia do alvo: metadados OIDC e JWKS do emissor (Cognito). A rotação de chaves é
/// automática, e o metadata só é buscado quando a primeira requisição autenticada chega.
/// </summary>
internal sealed class OidcAuthorityStrategy(string authority, string? audience) : EstrategiaDeJwt
{
    protected override void Aplicar(JwtBearerOptions options)
    {
        options.Authority = authority;
        // O emissor local de teste fala http; em produção o emissor é https e o metadata é exigido seguro.
        options.RequireHttpsMetadata = authority.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        options.Audience = audience;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidAudience = audience,
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    }
}

/// <summary>A estratégia do desenvolvimento: a chave simétrica configurada, como o <c>/dev/token</c> assina.</summary>
internal sealed class ChaveDeDesenvolvimentoStrategy(SymmetricSecurityKey chave, string? issuer, string? audience)
    : EstrategiaDeJwt
{
    protected override void Aplicar(JwtBearerOptions options) =>
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = chave,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero
        };
}
