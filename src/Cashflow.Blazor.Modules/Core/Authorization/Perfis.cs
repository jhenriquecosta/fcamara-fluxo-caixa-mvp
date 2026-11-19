using Microsoft.AspNetCore.Authorization;

namespace Cashflow.Blazor.Modules.Core.Authorization;

/// <summary>
/// Os perfis da tela (PRD-0003 §2). A tela esconde e bloqueia por policy por conveniência;
/// quem garante é a API, pelos escopos do token.
/// </summary>
public static class Perfis
{
    /// <summary>A claim da sessão que carrega o perfil.</summary>
    public const string ClaimType = "perfil";

    /// <summary>Lança, estorna e consulta.</summary>
    public const string Operador = "operador";

    /// <summary>Só consulta.</summary>
    public const string Consulta = "consulta";

    /// <summary>Os perfis válidos; um usuário com outro valor não entra.</summary>
    public static readonly string[] Todos = { Operador, Consulta };
}

/// <summary>As policies das telas, por perfil.</summary>
public static class Policies
{
    /// <summary>Lançar e estornar: só o operador.</summary>
    public const string PodeLancar = nameof(PodeLancar);

    /// <summary>Ver lançamentos, consolidado e infraestrutura: qualquer perfil válido.</summary>
    public const string PodeConsultar = nameof(PodeConsultar);


    /// <summary>Registra as duas policies no host.</summary>
    public static AuthorizationBuilder AddCashflowPolicies(this AuthorizationBuilder builder) =>
        builder
            .AddPolicy(PodeLancar, p => p.RequireAuthenticatedUser().RequireClaim(Perfis.ClaimType, Perfis.Operador))
            .AddPolicy(PodeConsultar, p => p.RequireAuthenticatedUser().RequireClaim(Perfis.ClaimType, Perfis.Todos));
}
