using System.Diagnostics;
using Cashflow.Blazor.Modules.Infraestrutura.Models;
using Microsoft.Extensions.Options;

namespace Cashflow.Blazor.Modules.Infraestrutura.Services;

/// <summary>Confere a saúde de cada serviço em paralelo; um serviço fora não atrasa os outros.</summary>
public class SaudeService(HttpClient httpClient, IOptions<InfraestruturaOptions> options)
{
    private static readonly TimeSpan Prazo = TimeSpan.FromSeconds(3);

    /// <summary>A saúde de todos os serviços configurados, na ordem da configuração.</summary>
    public async Task<IReadOnlyList<SaudeServico>> ChecarAsync(CancellationToken cancellationToken = default) =>
        await Task.WhenAll(options.Value.Servicos.Select(s => ChecarAsync(s, cancellationToken)));

    private async Task<SaudeServico> ChecarAsync(ServicoMonitorado servico, CancellationToken cancellationToken)
    {
        using var prazo = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        prazo.CancelAfter(Prazo);
        var relogio = Stopwatch.StartNew();
        try
        {
            using var resposta = await httpClient.GetAsync(servico.HealthUrl, prazo.Token);
            var status = (int)resposta.StatusCode;
            return new SaudeServico(servico.Nome, servico.Papel, resposta.IsSuccessStatusCode, status,
                relogio.Elapsed.TotalMilliseconds, resposta.IsSuccessStatusCode ? null : $"HTTP {status}");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new SaudeServico(servico.Nome, servico.Papel, false, null, relogio.Elapsed.TotalMilliseconds,
                $"sem resposta em {Prazo.TotalSeconds:0} s");
        }
        catch (HttpRequestException ex)
        {
            return new SaudeServico(servico.Nome, servico.Papel, false, null, relogio.Elapsed.TotalMilliseconds,
                ex.InnerException?.Message ?? ex.Message);
        }
    }
}
