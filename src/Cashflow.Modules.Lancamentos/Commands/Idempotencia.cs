using Cashflow.CrossCuttingConcerns.Security;
using Cashflow.Modules.Lancamentos.Models;
using Cashflow.Modules.Lancamentos.Repositories;
using Microsoft.Extensions.Logging;

namespace Cashflow.Modules.Lancamentos.Commands;

/// <summary>O que registro e estorno respondem quando a chave já foi usada — a mesma regra nos dois.</summary>
internal static class Idempotencia
{
    public static RegistroResultado? Desfecho(
        Reivindicacao reivindicacao, ILogger logger, string comercianteHash, string chave)
    {
        var chaveHash = Hashes.Chave(chave);
        switch (reivindicacao.Tipo)
        {
            case ReivindicacaoTipo.PayloadDivergente:
                logger.LogWarning("idempotência com payload divergente {ComercianteHash} {ChaveHash}",
                    comercianteHash, chaveHash);
                return new RegistroResultado.PayloadDivergente();
            case ReivindicacaoTipo.EmProcessamento:
                logger.LogWarning("requisição em processamento para a chave {ComercianteHash} {ChaveHash}",
                    comercianteHash, chaveHash);
                return new RegistroResultado.EmProcessamento();
            case ReivindicacaoTipo.Replay:
                logger.LogInformation("resposta idempotente devolvida {ComercianteHash} {ChaveHash}",
                    comercianteHash, chaveHash);
                return new RegistroResultado.Idempotente(reivindicacao.RespostaJson!);
            default:
                return null;
        }
    }
}
