using Cashflow.Blazor.Modules.Consolidado.Models;
using Cashflow.Blazor.Modules.Consolidado.Services;
using Cashflow.Blazor.Modules.Core.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Cashflow.Blazor.Modules.Core.Pages;

public partial class Index
{
    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    [Inject]
    public ConsolidadoService ConsolidadoService { get; set; } = default!;

    [Inject]
    public DiaDeNegocio DiaDeNegocio { get; set; } = default!;

    protected DateOnly Hoje { get; private set; }

    protected string Nome { get; private set; } = string.Empty;

    protected ConsolidadoDiarioModel? Consolidado { get; private set; }

    protected override async Task OnInitializedAsync()
    {
        Hoje = DiaDeNegocio.Hoje();
        var state = await AuthenticationStateTask;
        Nome = state.User.FindFirst("name")?.Value ?? state.User.Identity?.Name ?? string.Empty;
        await ExecutarAsync(async () => Consolidado = await ConsolidadoService.ObterAsync(Hoje));
    }
}
