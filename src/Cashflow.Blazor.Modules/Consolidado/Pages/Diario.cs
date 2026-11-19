using Cashflow.Blazor.Modules.Consolidado.Models;
using Cashflow.Blazor.Modules.Consolidado.Services;
using Cashflow.Blazor.Modules.Core.Services;
using Microsoft.AspNetCore.Components;

namespace Cashflow.Blazor.Modules.Consolidado.Pages;

public partial class Diario
{
    [Inject]
    public ConsolidadoService ConsolidadoService { get; set; } = default!;

    [Inject]
    public DiaDeNegocio DiaDeNegocio { get; set; } = default!;

    protected DateOnly Dia { get; private set; }

    protected ConsolidadoDiarioModel? Consolidado { get; private set; }

    protected override async Task OnInitializedAsync()
    {
        Dia = DiaDeNegocio.Hoje();
        await CarregarAsync();
    }

    protected async Task CarregarAsync() =>
        await ExecutarAsync(async () => Consolidado = await ConsolidadoService.ObterAsync(Dia));

    protected async Task TrocarDiaAsync(ChangeEventArgs e)
    {
        if (DateOnly.TryParse(e.Value?.ToString(), out var dia))
        {
            Dia = dia;
            Consolidado = null;
            await CarregarAsync();
        }
    }
}
