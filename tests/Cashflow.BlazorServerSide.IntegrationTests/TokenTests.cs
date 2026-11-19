using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Cashflow.Blazor.Modules.Core.Services;
using Cashflow.BlazorServerSide.ConfigurationOptions;
using Cashflow.BlazorServerSide.Services;
using Cashflow.Testing;
using Microsoft.IdentityModel.Tokens;

namespace Cashflow.BlazorServerSide.IntegrationTests;

/// <summary>ADR-0010: o token que o host emite carrega só o que o perfil pode, e vive pouco.</summary>
public sealed class TokenTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);

    private static AccessTokenIssuer Issuer() => new(new AppSettings
    {
        Auth = new AuthOptions
        {
            Issuer = TestTokens.Issuer,
            Audience = TestTokens.Audience,
            SigningKey = TestTokens.SigningKey,
            AccessTokenMinutes = 5
        }
    }, new FixedTimeProvider(Agora));

    private static JwtSecurityToken Ler(TokenModel token) => new JwtSecurityTokenHandler().ReadJwtToken(token.AccessToken);

    [Theory]
    [InlineData("operador", "lancamentos.write lancamentos.read consolidado.read")]
    [InlineData("consulta", "lancamentos.read consolidado.read")]
    [Trait("Criterio", "AC2")]
    public void O_token_carrega_so_os_escopos_do_perfil(string perfil, string escopos)
    {
        var usuario = UsuarioStore.Principal(new UsuarioOptions
        {
            Usuario = "u",
            Nome = "U",
            ComercianteId = FrontendFactory.Comerciante,
            Perfil = perfil
        });

        var jwt = Ler(Issuer().Emitir(usuario));

        Assert.Equal(escopos, jwt.Claims.Single(c => c.Type == "scope").Value);
        Assert.Equal(FrontendFactory.Comerciante.ToString(), jwt.Claims.Single(c => c.Type == "comercianteId").Value);
    }

    [Fact]
    [Trait("Criterio", "AC6")]
    public void O_token_vive_cinco_minutos_e_passa_na_validacao_da_api()
    {
        var token = Issuer().Emitir(UsuarioStore.Principal(FrontendFactory.Operador));

        Assert.Equal(Agora.AddMinutes(5), token.ExpiresAt);
        var jwt = Ler(token);
        Assert.Equal(TestTokens.Issuer, jwt.Issuer);
        Assert.Equal(TestTokens.Audience, Assert.Single(jwt.Audiences));
        new JwtSecurityTokenHandler().ValidateToken(token.AccessToken, new TokenValidationParameters
        {
            ValidIssuer = TestTokens.Issuer,
            ValidAudience = TestTokens.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestTokens.SigningKey)),
            ValidateLifetime = false
        }, out _);
    }

    [Fact]
    [Trait("Criterio", "AC2")]
    public void Perfil_desconhecido_nao_recebe_escopo()
    {
        Assert.Empty(AccessTokenIssuer.Escopos("admin"));
    }

    [Fact]
    [Trait("Criterio", "AC1")]
    public void A_senha_e_conferida_pelo_hash_e_perfil_invalido_nao_entra()
    {
        var store = new UsuarioStore(new AppSettings
        {
            Auth = new AuthOptions
            {
                Users =
                [
                    FrontendFactory.Operador,
                    new UsuarioOptions { Usuario = "x", SenhaHash = FrontendFactory.Operador.SenhaHash, Perfil = "admin" }
                ]
            }
        });

        Assert.NotNull(store.Autenticar("OPERADOR", FrontendFactory.Senha));
        Assert.Null(store.Autenticar("operador", "errada"));
        Assert.Null(store.Autenticar("x", FrontendFactory.Senha));
    }
}
