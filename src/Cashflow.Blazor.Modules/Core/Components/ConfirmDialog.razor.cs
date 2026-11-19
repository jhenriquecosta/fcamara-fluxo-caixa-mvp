using Microsoft.AspNetCore.Components;

namespace Cashflow.Blazor.Modules.Core.Components;

/// <summary>O ConfirmDialog da referência, sem JS: o modal é estado do componente.</summary>
public partial class ConfirmDialog
{
    [Parameter] public string Titulo { get; set; } = "Confirmar";
    [Parameter] public RenderFragment? Mensagem { get; set; }
    [Parameter] public string SimTexto { get; set; } = "Confirmar";
    [Parameter] public string NaoTexto { get; set; } = "Cancelar";
    [Parameter] public string CorConfirmar { get; set; } = "primary";
    [Parameter] public EventCallback AoConfirmar { get; set; }

    public bool Visivel { get; private set; }

    private bool Ocupado { get; set; }

    public void Mostrar()
    {
        Visivel = true;
        StateHasChanged();
    }

    public void Fechar()
    {
        Visivel = false;
        StateHasChanged();
    }

    private async Task ConfirmarAsync()
    {
        Ocupado = true;
        try
        {
            await AoConfirmar.InvokeAsync();
        }
        finally
        {
            Ocupado = false;
            Visivel = false;
        }
    }
}
