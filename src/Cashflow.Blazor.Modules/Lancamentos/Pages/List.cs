using Cashflow.Blazor.Modules.Core.Components;
using Cashflow.Blazor.Modules.Core.Services;
using Cashflow.Blazor.Modules.Lancamentos.Models;
using Cashflow.Blazor.Modules.Lancamentos.Services;
using Microsoft.AspNetCore.Components;

namespace Cashflow.Blazor.Modules.Lancamentos.Pages;

public partial class List
{
    /// <summary>
    /// Uma chave por intenção de estornar: o retry do mesmo estorno reusa a chave e a API devolve o
    /// mesmo 201 (ADR-0004); um estorno concluído libera a chave.
    /// </summary>
    private readonly Dictionary<Guid, string> _chavesDeEstorno = new();

    [Inject]
    public LancamentoService LancamentoService { get; set; } = default!;

    [Inject]
    public DiaDeNegocio DiaDeNegocio { get; set; } = default!;

    protected ConfirmDialog EstornoDialog { get; set; } = default!;

    protected DateOnly Dia { get; private set; }

    protected List<LancamentoModel>? Lancamentos { get; private set; }

    protected LancamentoModel? Estornando { get; private set; }

    protected string? Sucesso { get; set; }

    protected decimal Movimento => Lancamentos?.Sum(l => l.Efeito) ?? 0m;

    protected override async Task OnInitializedAsync()
    {
        Dia = DiaDeNegocio.Hoje();
        await CarregarAsync();
    }

    protected async Task CarregarAsync() =>
        await ExecutarAsync(async () => Lancamentos = await LancamentoService.ListarAsync(Dia));

    protected async Task TrocarDiaAsync(ChangeEventArgs e)
    {
        if (DateOnly.TryParse(e.Value?.ToString(), out var dia))
        {
            Dia = dia;
            Lancamentos = null;
            await CarregarAsync();
        }
    }

    protected void PedirEstorno(LancamentoModel lancamento)
    {
        Estornando = lancamento;
        Sucesso = null;
        EstornoDialog.Mostrar();
    }

    protected async Task ConfirmarEstornoAsync()
    {
        if (Estornando is not { } alvo)
            return;

        var chave = _chavesDeEstorno.TryGetValue(alvo.Id, out var existente)
            ? existente
            : _chavesDeEstorno[alvo.Id] = Guid.NewGuid().ToString();

        await ExecutarAsync(async () =>
        {
            await LancamentoService.EstornarAsync(alvo.Id, chave);
            _chavesDeEstorno.Remove(alvo.Id);
            Sucesso = $"Lançamento de {Core.Formatacao.Moeda(alvo.Valor)} estornado.";
            Lancamentos = await LancamentoService.ListarAsync(Dia);
        });
        Estornando = null;
    }
}
