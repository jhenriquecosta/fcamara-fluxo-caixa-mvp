using Cashflow.Lancamentos.Domain;
using Cashflow.Lancamentos.Domain.Ports;
using Microsoft.Extensions.Logging;

namespace Cashflow.Lancamentos.Application;

public sealed class EstornarLancamentoHandler(
    IRegistroDeLancamentos lancamentos,
    IPortDeIdempotencia idempotencia,
    TimeProvider clock,
    TimeZoneInfo fuso,
    ILogger<EstornarLancamentoHandler> logger)
{
    public async Task<RegistroResultado> HandleAsync(EstornarLancamentoCommand comando, CancellationToken ct)
    {
        var hash = comando.LancamentoId.ToString();
        var comercianteHash = LancamentoCorpo.HashComerciante(comando.ComercianteId);
        var chaveHash = LancamentoCorpo.HashChave(comando.IdempotencyKey);

        var claim = await idempotencia.ReivindicarAsync(
            comando.ComercianteId, comando.IdempotencyKey, hash, ct);

        switch (claim.Tipo)
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
                logger.LogInformation("estorno idempotente devolvido {ComercianteHash} {ChaveHash}",
                    comercianteHash, chaveHash);
                return new RegistroResultado.Idempotente(claim.RespostaJson!);
        }

        var agora = clock.GetUtcNow();
        var data = DiaDeNegocio.De(agora, fuso);

        try
        {
            var (resultado, estorno) = await lancamentos.EstornarAsync(
                comando.ComercianteId, comando.LancamentoId, data, agora, comando.IdempotencyKey, ct);

            switch (resultado)
            {
                case EstornoResultado.Criado:
                    logger.LogInformation("estorno registrado {LancamentoId} {EstornoDe} {ComercianteHash}",
                        estorno!.Id, comando.LancamentoId, comercianteHash);
                    return new RegistroResultado.Criado(new LancamentoResponse(
                        estorno.Id, estorno.ComercianteId, estorno.Data, estorno.Valor,
                        estorno.Tipo.Texto(), null, estorno.EstornoDe));
                case EstornoResultado.NaoEncontrado:
                    await idempotencia.FalharAsync(comando.ComercianteId, comando.IdempotencyKey, ct);
                    return new RegistroResultado.NaoEncontrado();
                case EstornoResultado.RegraInvalida:
                    await idempotencia.FalharAsync(comando.ComercianteId, comando.IdempotencyKey, ct);
                    return new RegistroResultado.RegraInvalida();
                default:
                    await idempotencia.FalharAsync(comando.ComercianteId, comando.IdempotencyKey, ct);
                    return new RegistroResultado.Conflito();
            }
        }
        catch (Exception ex)
        {
            await idempotencia.FalharAsync(comando.ComercianteId, comando.IdempotencyKey, ct);
            logger.LogError(ex, "falha ao estornar lançamento {EstornoDe} {ComercianteHash}",
                comando.LancamentoId, comercianteHash);
            throw;
        }
    }
}
