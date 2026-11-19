using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace Cashflow.Infrastructure.Security;

public static class CashflowAuth
{
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

    public static bool HasScope(ClaimsPrincipal user, string scope) =>
        user.FindFirst("scope")?.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(scope) == true;

    public static Guid? ComercianteId(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirst("comercianteId")?.Value, out var id) ? id : null;
}

public static class Scopes
{
    public const string LancamentosWrite = "lancamentos.write";
    public const string ConsolidadoRead = "consolidado.read";

    public static readonly string[] Todos = { LancamentosWrite, ConsolidadoRead };
}
