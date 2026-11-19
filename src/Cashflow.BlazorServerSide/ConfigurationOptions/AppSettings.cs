namespace Cashflow.BlazorServerSide.ConfigurationOptions;

/// <summary>A configuração do host do frontend.</summary>
public sealed class AppSettings
{
    /// <summary>O nome nos logs e nas métricas.</summary>
    public string ServiceName { get; set; } = "web";


    /// <summary>Os endereços da API.</summary>
    public ResourceServerOptions ResourceServer { get; set; } = new();


    /// <summary>Login e emissão do token de acesso.</summary>
    public AuthOptions Auth { get; set; } = new();
}

/// <summary>Um endereço por serviço da API: o compose sobe lancamentos-api e consolidado-api separados.</summary>
public sealed class ResourceServerOptions
{
    /// <summary>O lancamentos-api.</summary>
    public string Lancamentos { get; set; } = "http://localhost:8080";

    /// <summary>O consolidado-api.</summary>
    public string Consolidado { get; set; } = "http://localhost:8081";
}

/// <summary>
/// O host faz o papel de IdP local (ADR-0010): emite tokens com o issuer, a audience e a chave que a
/// API valida. No alvo, isto vira a configuração OIDC e a chave sai daqui.
/// </summary>
public sealed class AuthOptions
{
    /// <summary>O <c>iss</c> que a API valida.</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>O <c>aud</c> que a API valida.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>A chave HMAC compartilhada com a API (só local: ADR-0010).</summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>A vida do token emitido.</summary>
    public int AccessTokenMinutes { get; set; } = 5;

    /// <summary>Os usuários que podem entrar.</summary>
    public List<UsuarioOptions> Users { get; set; } = new();
}

/// <summary>Um usuário configurado.</summary>
public sealed class UsuarioOptions
{
    /// <summary>O login, sem diferença de maiúsculas.</summary>
    public string Usuario { get; set; } = string.Empty;

    /// <summary>O nome exibido.</summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>Hash PBKDF2 no formato do <c>PasswordHasher</c> do ASP.NET Core Identity — nunca a senha.</summary>
    public string SenhaHash { get; set; } = string.Empty;

    /// <summary>O comerciante a que o usuário pertence; vai no token.</summary>
    public Guid ComercianteId { get; set; }

    /// <summary><c>operador</c> ou <c>consulta</c>.</summary>
    public string Perfil { get; set; } = string.Empty;
}
