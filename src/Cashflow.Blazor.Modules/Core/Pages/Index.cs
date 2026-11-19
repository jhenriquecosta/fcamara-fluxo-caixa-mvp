using Cashflow.Blazor.Modules.Consolidado.Models;
using Cashflow.Blazor.Modules.Consolidado.Services;
using Cashflow.Blazor.Modules.Core.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Cashflow.Blazor.Modules.Core.Pages;

/// <summary>O início: o consolidado de hoje e os atalhos que o perfil permite.</summary>
public partial class Index
{
    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    [Inject]
    private ConsolidadoService ConsolidadoService { get; set; } = default!;

    [Inject]
    private DiaDeNegocio DiaDeNegocio { get; set; } = default!;

    private DateOnly Hoje { get; set; }

    private string Nome { get; set; } = string.Empty;

    private ConsolidadoDiarioModel? Consolidado { get; set; }

    /// <inheritdoc />

    protected override async Task OnInitializedAsync()
    {
        Hoje = DiaDeNegocio.Hoje();
        var state = await AuthenticationStateTask;
        Nome = state.User.FindFirst("name")?.Value ?? state.User.Identity?.Name ?? string.Empty;
        await ExecutarAsync(async () => Consolidado = await ConsolidadoService.ObterAsync(Hoje));
    }
}
