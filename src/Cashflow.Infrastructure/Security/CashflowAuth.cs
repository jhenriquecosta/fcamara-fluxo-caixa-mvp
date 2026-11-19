using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Cashflow.Infrastructure.Security;

/// <summary>
/// A autenticação e a autorização das APIs: JWT Bearer validado por emissor, audiência, assinatura e
/// validade (sem tolerância de relógio), e uma política por escopo (<c>lancamentos.write</c>,
/// <c>lancamentos.read</c>, <c>consolidado.read</c>). O comerciante vem do token, nunca do corpo: um comerciante não grava no caixa de outro.
/// </summary>
public static class CashflowAuth
{
    /// <summary>
    /// Registra o JWT Bearer com a estratégia que a configuração pedir (ADR-0012): JWKS do emissor
    /// quando <c>Auth:Authority</c> existe, chave simétrica local quando não. As políticas de escopo
    /// são as mesmas nas duas.
    /// </summary>
    public static IServiceCollection AddCashflowAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        var authority = configuration["Auth:Authority"];
        if (!string.IsNullOrWhiteSpace(authority))
        {
            // O alvo: metadados OIDC e JWKS do emissor (Cognito). A chave simétrica não participa.
            services.AddSingleton<IConfigureOptions<JwtBearerOptions>>(
                new OidcAuthorityStrategy(authority, configuration["Auth:Audience"]));
        }
        else
        {
            var signingKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration["Auth:SigningKey"]
                    ?? throw new InvalidOperationException("Auth:SigningKey não configurada")));
            services.AddSingleton<IConfigureOptions<JwtBearerOptions>>(
                new ChaveDeDesenvolvimentoStrategy(
                    signingKey, configuration["Auth:Issuer"], configuration["Auth:Audience"]));
        }

        services.AddAuthorization(options =>
        {
            options.AddPolicy(Scopes.LancamentosWrite, policy =>
                policy.RequireAssertion(context => HasScope(context.User, Scopes.LancamentosWrite)));
            options.AddPolicy(Scopes.LancamentosRead, policy =>
                policy.RequireAssertion(context => HasScope(context.User, Scopes.LancamentosRead)));
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

    /// <summary>Listar os lançamentos do dia (a tela de estorno e o perfil consulta, ADR-0010).</summary>
    public const string LancamentosRead = "lancamentos.read";

    /// <summary>Consultar o consolidado diário.</summary>
    public const string ConsolidadoRead = "consolidado.read";

    /// <summary>Todos os escopos; é o que o <c>/dev/token</c> emite quando o pedido não diz quais.</summary>
    public static readonly string[] Todos = { LancamentosWrite, LancamentosRead, ConsolidadoRead };
}
