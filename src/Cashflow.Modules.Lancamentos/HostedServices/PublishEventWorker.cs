using Cashflow.Application;
using Cashflow.Modules.Lancamentos.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cashflow.Modules.Lancamentos.HostedServices;

/// <summary>
/// O relógio do outbox: despacha o <see cref="PublishEventsCommand"/> em laço, como na referência.
/// Não tem regra; o trabalho é do handler. Roda no <c>BackgroundServer</c>, nunca na API.
/// </summary>
public sealed class PublishEventWorker(IServiceProvider services, ILogger<PublishEventWorker> logger) : BackgroundService
{
    /// <summary>Pausa quando o outbox está vazio, para não martelar o banco.</summary>
    private static readonly TimeSpan EsperaComOutboxVazio = TimeSpan.FromMilliseconds(500);

    /// <summary>Pausa depois de uma falha (broker ou banco fora), antes de tentar de novo.</summary>
    private static readonly TimeSpan EsperaAposFalha = TimeSpan.FromSeconds(1);

    /// <summary>Publica lotes até o host parar. Com lote cheio emenda o próximo sem esperar; uma falha nunca derruba o laço.</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("publicador do outbox iniciado");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var command = new PublishEventsCommand();
                await using (var scope = services.CreateAsyncScope())
                {
                    var dispatcher = scope.ServiceProvider.GetRequiredService<Dispatcher>();
                    await dispatcher.DispatchAsync(command, stoppingToken);
                }

                if (command.SentEventsCount == 0)
                    await Task.Delay(EsperaComOutboxVazio, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "falha no publicador do outbox; tentando de novo em {Espera}", EsperaAposFalha);
                try { await Task.Delay(EsperaAposFalha, stoppingToken); }
                catch (OperationCanceledException) { break; }
            }
        }
    }
}
