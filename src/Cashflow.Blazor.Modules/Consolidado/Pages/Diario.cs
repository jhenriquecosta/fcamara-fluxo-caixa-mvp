using Cashflow.Blazor.Modules.Consolidado.Models;
using Cashflow.Blazor.Modules.Consolidado.Services;
using Cashflow.Blazor.Modules.Core.Services;
using Microsoft.AspNetCore.Components;

namespace Cashflow.Blazor.Modules.Consolidado.Pages;

/// <summary>O consolidado de uma data: saldo do dia, anterior e acumulado.</summary>
public partial class Diario
{
    [Inject]
    private ConsolidadoService ConsolidadoService { get; set; } = default!;

    [Inject]
    private DiaDeNegocio DiaDeNegocio { get; set; } = default!;

    private DateOnly Dia { get; set; }

    private ConsolidadoDiarioModel? Consolidado { get; set; }

    /// <inheritdoc />

    protected override async Task OnInitializedAsync()
    {
        Dia = DiaDeNegocio.Hoje();
        await CarregarAsync();
    }

    private async Task CarregarAsync() =>
        await ExecutarAsync(async () => Consolidado = await ConsolidadoService.ObterAsync(Dia));

    private async Task TrocarDiaAsync(ChangeEventArgs e)
    {
        if (DateOnly.TryParse(e.Value?.ToString(), out var dia))
        {
            Dia = dia;
            Consolidado = null;
            await CarregarAsync();
        }
    }
}
