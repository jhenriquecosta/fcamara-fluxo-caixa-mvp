using Cashflow.Application;
using Cashflow.Modules.Lancamentos.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cashflow.Modules.Lancamentos.HostedServices;

/// <summary>Despacha o <see cref="PublishEventsCommand"/> em laço; o trabalho é do handler, como na referência.</summary>
public sealed class PublishEventWorker(IServiceProvider services, ILogger<PublishEventWorker> logger) : BackgroundService
{
    private static readonly TimeSpan EsperaSemEventos = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan EsperaAposFalha = TimeSpan.FromSeconds(1);

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
                    await Task.Delay(EsperaSemEventos, stoppingToken);
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
