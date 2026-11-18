using System.Text.Json;
using Cashflow.CrossCuttingConcerns.Security;
using Cashflow.Contracts;
using Cashflow.Modules.Lancamentos.Entities;
using Cashflow.Modules.Lancamentos.Repositories;
using Microsoft.Extensions.Logging;

namespace Cashflow.Modules.Lancamentos.Commands;

public sealed class RegistrarLancamentoHandler(
    IRegistroDeLancamentos lancamentos,
    IPortDeIdempotencia idempotencia,
    TimeProvider clock,
    TimeZoneInfo fuso,
    ILogger<RegistrarLancamentoHandler> logger)
{
    public async Task<RegistroResultado> HandleAsync(RegistrarLancamentoCommand comando, CancellationToken ct)
    {
        var hash = LancamentoCorpo.Hash(comando.Tipo, comando.Valor, comando.Descricao);
        var comercianteHash = Hashes.Comerciante(comando.ComercianteId);
        var chaveHash = Hashes.Chave(comando.IdempotencyKey);

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
                logger.LogInformation("lançamento idempotente devolvido {ComercianteHash} {ChaveHash}",
                    comercianteHash, chaveHash);
                return new RegistroResultado.Idempotente(claim.RespostaJson!);
        }

        var agora = clock.GetUtcNow();
        var data = DiaDeNegocio.De(agora, fuso);
        var lancamento = Lancamento.Registrar(
            comando.ComercianteId, comando.Tipo, comando.Valor, comando.Descricao, data, agora);
        var resposta = new LancamentoResponse(
            lancamento.Id, lancamento.ComercianteId, lancamento.Data,
            lancamento.Valor, lancamento.Tipo.Texto(), lancamento.Descricao, null);
        var respostaJson = JsonSerializer.Serialize(resposta, CashflowJson.Options);

        try
        {
            await lancamentos.RegistrarAsync(lancamento, respostaJson, comando.IdempotencyKey, ct);
        }
        catch (Exception ex)
        {
            await idempotencia.FalharAsync(comando.ComercianteId, comando.IdempotencyKey, ct);
            logger.LogError(ex, "falha ao registrar lançamento {ComercianteHash}", comercianteHash);
            throw;
        }

        logger.LogInformation("lançamento registrado {LancamentoId} {ComercianteHash} {Tipo} {Valor}",
            lancamento.Id, comercianteHash, lancamento.Tipo.Texto(), lancamento.Valor);

        return new RegistroResultado.Criado(resposta);
    }
}
