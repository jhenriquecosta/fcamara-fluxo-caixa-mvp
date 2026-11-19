using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace Cashflow.Infrastructure.Security;

/// <summary>
/// A autenticação e a autorização das APIs: JWT Bearer validado por emissor, audiência, assinatura e
/// validade (sem tolerância de relógio), e uma política por escopo (<c>lancamentos.write</c>,
/// <c>consolidado.read</c>). O comerciante vem do token, nunca do corpo: um comerciante não grava no caixa de outro.
/// </summary>
public static class CashflowAuth
{
    /// <summary>Registra o JWT Bearer e as políticas de escopo. Falhas de autenticação e autorização vão ao log como aviso.</summary>
    public static IServiceCollection AddCashflowAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Auth:SigningKey"]
                ?? throw new InvalidOperationException("Auth:SigningKey não configurada")));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = configuration["Auth:Issuer"],
                    ValidAudience = configuration["Auth:Audience"],
                    IssuerSigningKey = signingKey,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.Zero
                };
                options.Events = new JwtBearerEvents
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
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(Scopes.LancamentosWrite, policy =>
                policy.RequireAssertion(context => HasScope(context.User, Scopes.LancamentosWrite)));
            options.AddPolicy(Scopes.ConsolidadoRead, policy =>
                policy.RequireAssertion(context => HasScope(context.User, Scopes.ConsolidadoRead)));
        });

        return services;
    }

    /// <summary>Diz se a claim <c>scope</c> do token (escopos separados por espaço, como no OAuth 2) contém <paramref name="scope"/>.</summary>
    public static bool HasScope(ClaimsPrincipal user, string scope) =>
        user.FindFirst("scope")?.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(scope) == true;

    /// <summary>O comerciante dono do token (claim <c>comercianteId</c>), ou <c>null</c> se o token não o traz.</summary>
    public static Guid? ComercianteId(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirst("comercianteId")?.Value, out var id) ? id : null;
}

/// <summary>Os escopos do token. Cada rota exige um, e o token só carrega os que o cliente pode usar.</summary>
public static class Scopes
{
    /// <summary>Registrar e estornar lançamentos.</summary>
    public const string LancamentosWrite = "lancamentos.write";

    /// <summary>Consultar o consolidado diário.</summary>
    public const string ConsolidadoRead = "consolidado.read";

    /// <summary>Todos os escopos; é o que o <c>/dev/token</c> emite quando o pedido não diz quais.</summary>
    public static readonly string[] Todos = { LancamentosWrite, ConsolidadoRead };
}
