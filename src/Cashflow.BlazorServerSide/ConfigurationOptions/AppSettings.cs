namespace Cashflow.BlazorServerSide.ConfigurationOptions;

public sealed class AppSettings
{
    public string ServiceName { get; set; } = "web";

    public ResourceServerOptions ResourceServer { get; set; } = new();

    public AuthOptions Auth { get; set; } = new();
}

/// <summary>Um endereço por serviço da API: o compose sobe lancamentos-api e consolidado-api separados.</summary>
public sealed class ResourceServerOptions
{
    public string Lancamentos { get; set; } = "http://localhost:8080";

    public string Consolidado { get; set; } = "http://localhost:8081";
}

/// <summary>
/// O host faz o papel de IdP local (ADR-0010): emite tokens com o issuer, a audience e a chave que a
/// API valida. No alvo, isto vira a configuração OIDC e a chave sai daqui.
/// </summary>
public sealed class AuthOptions
{
    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 5;

    public List<UsuarioOptions> Users { get; set; } = new();
}

public sealed class UsuarioOptions
{
    public string Usuario { get; set; } = string.Empty;

    public string Nome { get; set; } = string.Empty;

    /// <summary>Hash PBKDF2 no formato do <c>PasswordHasher</c> do ASP.NET Core Identity — nunca a senha.</summary>
    public string SenhaHash { get; set; } = string.Empty;

    public Guid ComercianteId { get; set; }

    public string Perfil { get; set; } = string.Empty;
}
