using Cashflow.Blazor.Modules.Infraestrutura.Models;
using Cashflow.Blazor.Modules.Infraestrutura.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

namespace Cashflow.Blazor.Modules.Infraestrutura.Pages;

/// <summary>
/// O painel de infraestrutura (PRD-0004 RF6): cada bloco consulta a sua fonte e degrada sozinho;
/// o Elasticsearch fora não apaga a saúde nem os sinais.
/// </summary>
public partial class Painel
{
    private readonly CancellationTokenSource _vida = new();
    private PeriodicTimer? _timer;

    /// <summary>A configuração do painel.</summary>
    [Inject]
    public IOptions<InfraestruturaOptions> Opcoes { get; set; } = default!;

    /// <summary>O cliente de saúde.</summary>
    [Inject]
    public SaudeService SaudeService { get; set; } = default!;

    /// <summary>O cliente do Prometheus.</summary>
    [Inject]
    public PrometheusService PrometheusService { get; set; } = default!;

    /// <summary>O cliente dos logs.</summary>
    [Inject]
    public LogsService LogsService { get; set; } = default!;

    private IReadOnlyList<SaudeServico>? Saude { get; set; }
    private SinaisModel? Sinais { get; set; }
    private string? ErroSinais { get; set; }
    private IReadOnlyList<LogEntrada>? Logs { get; set; }
    private string? ErroLogs { get; set; }
    private string FiltroServico { get; set; } = string.Empty;
    private string FiltroNivel { get; set; } = string.Empty;
    private DateTimeOffset? Atualizado { get; set; }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        if (!Opcoes.Value.Enabled)
            return;
        await AtualizarAsync();
    }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        // O timer só existe no circuito interativo, não na pré-renderização.
        if (firstRender && Opcoes.Value.Enabled && RendererInfo.IsInteractive)
            _ = AtualizarPeriodicamenteAsync();
    }

    private async Task AtualizarPeriodicamenteAsync()
    {
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        try
        {
            while (await _timer.WaitForNextTickAsync(_vida.Token))
            {
                await AtualizarAsync();
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException)
        {
            // a página fechou
        }
    }

    private async Task AtualizarAsync()
    {
        var saude = SaudeService.ChecarAsync(_vida.Token);
        var sinais = CarregarSinaisAsync();
        var logs = CarregarLogsAsync();
        await Task.WhenAll(saude, sinais, logs);
        Saude = saude.Result;
        Atualizado = DateTimeOffset.UtcNow;
    }

    private async Task CarregarSinaisAsync()
    {
        try
        {
            Sinais = await PrometheusService.ObterSinaisAsync(_vida.Token);
            ErroSinais = null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException
                                       or System.Text.Json.JsonException)
        {
            ErroSinais = "Prometheus indisponível — os sinais voltam quando ele voltar.";
        }
    }

    private async Task CarregarLogsAsync()
    {
        try
        {
            Logs = await LogsService.UltimosAsync(30, Vazio(FiltroServico), Vazio(FiltroNivel), _vida.Token);
            ErroLogs = null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException
                                       or KeyNotFoundException)
        {
            ErroLogs = "Elasticsearch indisponível — os logs voltam quando ele voltar.";
        }
    }

    private static string? Vazio(string texto) => string.IsNullOrEmpty(texto) ? null : texto;

    private static string Segundos(double? valor) => valor is { } v ? $"{v:0.0} s" : "—";

    /// <inheritdoc />
    public void Dispose()
    {
        _vida.Cancel();
        _timer?.Dispose();
        _vida.Dispose();
    }
}
