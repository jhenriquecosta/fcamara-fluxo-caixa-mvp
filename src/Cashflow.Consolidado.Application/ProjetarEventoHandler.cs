using System.Security.Cryptography;
using Cashflow.Consolidado.Domain;
using Cashflow.Consolidado.Domain.Ports;
using Microsoft.Extensions.Logging;

namespace Cashflow.Consolidado.Application;

public sealed class ProjetarEventoHandler(
    IProjecaoConsolidado projecao,
    ILogger<ProjetarEventoHandler> logger)
{
    public async Task HandleAsync(LancamentoEvento evento, CancellationToken ct)
    {
        var resultado = await projecao.AplicarAsync(evento, ct);
        var comercianteHash = HashComerciante(evento.ComercianteId);

        if (resultado == ProjecaoResultado.Duplicado)
        {
            logger.LogInformation("evento repetido ignorado {EventId} {ComercianteHash}",
                evento.EventId, comercianteHash);
            return;
        }

        logger.LogInformation("evento aplicado {EventId} {ComercianteHash} {Data} {Delta}",
            evento.EventId, comercianteHash, evento.Data, evento.Delta);
    }

    private static string HashComerciante(Guid id) =>
        Convert.ToHexString(SHA256.HashData(id.ToByteArray()))[..16].ToLowerInvariant();
}
