using Cashflow.CrossCuttingConcerns.Security;
using Cashflow.Modules.Consolidado.Entities;
using Cashflow.Modules.Consolidado.Repositories;
using Microsoft.Extensions.Logging;

namespace Cashflow.Modules.Consolidado.Commands;

public sealed class ProjetarEventoHandler(
    IProjecaoConsolidado projecao,
    ILogger<ProjetarEventoHandler> logger)
{
    public async Task HandleAsync(LancamentoEvento evento, CancellationToken ct)
    {
        var resultado = await projecao.AplicarAsync(evento, ct);
        var comercianteHash = Hashes.Comerciante(evento.ComercianteId);

        if (resultado == ProjecaoResultado.Duplicado)
        {
            logger.LogInformation("evento repetido ignorado {EventId} {ComercianteHash}",
                evento.EventId, comercianteHash);
            return;
        }

        logger.LogInformation("evento aplicado {EventId} {ComercianteHash} {Data} {Delta}",
            evento.EventId, comercianteHash, evento.Data, evento.Delta);
    }
}
