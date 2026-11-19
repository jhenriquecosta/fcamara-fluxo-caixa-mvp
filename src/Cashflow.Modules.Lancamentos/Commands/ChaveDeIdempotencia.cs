using Cashflow.CrossCuttingConcerns.Security;
using Cashflow.Modules.Lancamentos.Models;
using Cashflow.Modules.Lancamentos.Repositories;
using Microsoft.Extensions.Logging;

namespace Cashflow.Modules.Lancamentos.Commands;

/// <summary>A decisão comum ao registro e ao estorno quando a chave de idempotência não é desta requisição.</summary>
internal static class ChaveDeIdempotencia
{
    /// <summary>
    /// A resposta para uma chave que já tem dono, ou <c>null</c> quando a chave foi reservada agora e o
    /// caso de uso deve executar. Cada recusa vai ao log com o comerciante e a chave pseudonimizados.
    /// </summary>
    public static ResultadoDoLancamento? RespostaSeJaUsada(
        ReservaDaChave reserva, ILogger logger, string comercianteNoLog, string chave)
    {
        var chaveNoLog = PseudonimoDeLog.DaChaveDeIdempotencia(chave);
        switch (reserva.Situacao)
        {
            case SituacaoDaChave.UsadaComOutroPedido:
                logger.LogWarning("idempotência com payload divergente {ComercianteHash} {ChaveHash}",
                    comercianteNoLog, chaveNoLog);
                return new ResultadoDoLancamento.ChaveUsadaComOutroPedido();
            case SituacaoDaChave.EmProcessamento:
                logger.LogWarning("requisição em processamento para a chave {ComercianteHash} {ChaveHash}",
                    comercianteNoLog, chaveNoLog);
                return new ResultadoDoLancamento.ChaveEmProcessamento();
            case SituacaoDaChave.JaConcluida:
                logger.LogInformation("resposta idempotente devolvida {ComercianteHash} {ChaveHash}",
                    comercianteNoLog, chaveNoLog);
                return new ResultadoDoLancamento.RespostaRepetida(reserva.RespostaGravada!);
            default:
                return null;
        }
    }
}
