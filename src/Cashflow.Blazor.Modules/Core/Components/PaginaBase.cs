using Cashflow.Blazor.Modules.Core.Services;
using Microsoft.AspNetCore.Components;

namespace Cashflow.Blazor.Modules.Core.Components;

/// <summary>
/// O tratamento de erro comum às telas: a mensagem da API vira alerta, e a sessão expirada
/// (401 da API) leva ao login com recarga, porque o cookie só se renova numa requisição HTTP.
/// </summary>
public abstract class PaginaBase : ComponentBase
{
    /// <summary>Para levar ao login quando a sessão expira.</summary>
    [Inject]
    public NavigationManager NavManager { get; set; } = default!;

    /// <summary>A mensagem de erro da última ação, pronta para o alerta.</summary>
    protected string? Erro { get; set; }

    /// <summary>Há uma ação em andamento: os botões ficam desabilitados.</summary>
    protected bool Ocupado { get; private set; }

    /// <summary>Executa a ação traduzindo os erros da API: 401 vai ao login, o resto vira <see cref="Erro"/>.</summary>
    protected async Task ExecutarAsync(Func<Task> acao)
    {
        Erro = null;
        Ocupado = true;
        try
        {
            await acao();
        }
        catch (ApiException ex) when (ex.SessaoExpirada)
        {
            var retorno = Uri.EscapeDataString("/" + NavManager.ToBaseRelativePath(NavManager.Uri));
            NavManager.NavigateTo($"/login?ReturnUrl={retorno}", forceLoad: true);
        }
        catch (ApiException ex)
        {
            Erro = ex.Detalhe is null ? Capitalizar(ex.Titulo) : $"{Capitalizar(ex.Titulo)} — {ex.Detalhe}.";
        }
        finally
        {
            Ocupado = false;
        }
    }

    private static string Capitalizar(string texto) =>
        string.IsNullOrEmpty(texto) ? texto : char.ToUpperInvariant(texto[0]) + texto[1..];
}
