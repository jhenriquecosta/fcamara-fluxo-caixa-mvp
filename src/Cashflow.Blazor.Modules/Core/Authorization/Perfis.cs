using Microsoft.AspNetCore.Authorization;

namespace Cashflow.Blazor.Modules.Core.Authorization;

/// <summary>
/// Os perfis da tela (PRD-0003 §2). A tela esconde e bloqueia por policy por conveniência;
/// quem garante é a API, pelos escopos do token.
/// </summary>
public static class Perfis
{
    public const string ClaimType = "perfil";
    public const string Operador = "operador";
    public const string Consulta = "consulta";

    public static readonly string[] Todos = { Operador, Consulta };
}

public static class Policies
{
    public const string PodeLancar = nameof(PodeLancar);
    public const string PodeConsultar = nameof(PodeConsultar);

    public static AuthorizationBuilder AddCashflowPolicies(this AuthorizationBuilder builder) =>
        builder
            .AddPolicy(PodeLancar, p => p.RequireAuthenticatedUser().RequireClaim(Perfis.ClaimType, Perfis.Operador))
            .AddPolicy(PodeConsultar, p => p.RequireAuthenticatedUser().RequireClaim(Perfis.ClaimType, Perfis.Todos));
}
