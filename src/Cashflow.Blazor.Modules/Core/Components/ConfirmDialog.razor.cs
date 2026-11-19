using Microsoft.AspNetCore.Components;

namespace Cashflow.Blazor.Modules.Core.Components;

/// <summary>O ConfirmDialog da referência, sem JS: o modal é estado do componente.</summary>
public partial class ConfirmDialog
{
    /// <summary>O título do modal.</summary>
    [Parameter] public string Titulo { get; set; } = "Confirmar";

    /// <summary>O corpo: o que vai acontecer se confirmar.</summary>
    [Parameter] public RenderFragment? Mensagem { get; set; }

    /// <summary>O texto do botão que confirma.</summary>
    [Parameter] public string SimTexto { get; set; } = "Confirmar";

    /// <summary>O texto do botão que desiste.</summary>
    [Parameter] public string NaoTexto { get; set; } = "Cancelar";

    /// <summary>A cor Bootstrap do botão que confirma (<c>danger</c> para o que não se desfaz).</summary>
    [Parameter] public string CorConfirmar { get; set; } = "primary";

    /// <summary>A ação confirmada; o modal fecha quando ela termina.</summary>
    [Parameter] public EventCallback AoConfirmar { get; set; }

    /// <summary>O modal está aberto.</summary>
    public bool Visivel { get; private set; }

    private bool Ocupado { get; set; }

    /// <summary>Abre o modal.</summary>
    public void Mostrar()
    {
        Visivel = true;
        StateHasChanged();
    }

    /// <summary>Fecha o modal sem confirmar.</summary>
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
