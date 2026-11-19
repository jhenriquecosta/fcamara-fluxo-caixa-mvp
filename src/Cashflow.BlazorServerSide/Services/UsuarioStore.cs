using System.Security.Claims;
using Cashflow.Blazor.Modules.Core.Authorization;
using Cashflow.BlazorServerSide.ConfigurationOptions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

namespace Cashflow.BlazorServerSide.Services;

/// <summary>Os usuários configurados (PRD-0003 §2). O MVP não tem cadastro de pessoas.</summary>
public sealed class UsuarioStore(AppSettings settings)
{
    private static readonly PasswordHasher<UsuarioOptions> Hasher = new();

    /// <summary>Hash de uma senha qualquer: usuário inexistente custa o mesmo PBKDF2 que um existente.</summary>
    private static readonly string HashFicticio = Hasher.HashPassword(new UsuarioOptions(), Guid.NewGuid().ToString());

    /// <summary>O usuário, se a senha confere e o perfil é válido; senão nulo.</summary>
    public UsuarioOptions? Autenticar(string? usuario, string? senha)
    {
        if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrEmpty(senha))
            return null;

        var encontrado = settings.Auth.Users.FirstOrDefault(u =>
            string.Equals(u.Usuario, usuario.Trim(), StringComparison.OrdinalIgnoreCase));

        var resultado = Hasher.VerifyHashedPassword(encontrado ?? new UsuarioOptions(),
            encontrado?.SenhaHash ?? HashFicticio, senha);

        return encontrado is not null && resultado != PasswordVerificationResult.Failed
               && Perfis.Todos.Contains(encontrado.Perfil)
            ? encontrado
            : null;
    }

    /// <summary>O que vai no cookie de sessão: identidade e perfil, nunca o token da API.</summary>
    public static ClaimsPrincipal Principal(UsuarioOptions usuario) =>
        new(new ClaimsIdentity(
        [
            new Claim("sub", usuario.Usuario),
            new Claim("name", usuario.Nome),
            new Claim("comercianteId", usuario.ComercianteId.ToString()),
            new Claim(Perfis.ClaimType, usuario.Perfil)
        ], CookieAuthenticationDefaults.AuthenticationScheme, "name", Perfis.ClaimType));
}
