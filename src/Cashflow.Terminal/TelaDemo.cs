using Cashflow.Blazor.Modules.Consolidado.Models;
using Cashflow.Blazor.Modules.Core;
using Cashflow.Blazor.Modules.Core.Services;
using Cashflow.Blazor.Modules.Lancamentos.Models;
using Spectre.Console;

namespace Cashflow.Terminal;

/// <summary>A demo ponta a ponta: lançar, ver o consolidado, estornar e provar a idempotência.</summary>
public sealed class TelaDemo(IAnsiConsole console, Sessao sessao)
{
    /// <summary>O crédito que abre a demo.</summary>
    public const decimal Credito = 1500m;

    /// <summary>O débito que acompanha o crédito.</summary>
    public const decimal Debito = 320.50m;

    /// <summary>Executa o roteiro narrado contra a stack.</summary>
    /// <param name="cancellationToken">Cancelamento.</param>
    public async Task ExecutarAsync(CancellationToken cancellationToken = default)
    {
        if (!sessao.PodeLancar)
        {
            console.WriteLine("A demo lança e estorna — exige o perfil operador.");
            return;
        }

        console.WriteLine("Demo ponta a ponta: lançamentos → outbox → Kafka → consolidado → estorno → idempotência.");
        try
        {
            console.WriteLine($"1/6 Lançando crédito de {Formatacao.Moeda(Credito)}...");
            var credito = await sessao.Lancamentos.LancarAsync(
                new NovoLancamentoModel { Tipo = "credito", Valor = Credito, Descricao = "Demo — crédito" },
                Guid.NewGuid().ToString(), cancellationToken);
            console.WriteLine($"    lançamento {credito.Id} registrado");

            console.WriteLine($"2/6 Lançando débito de {Formatacao.Moeda(Debito)}...");
            var debito = await sessao.Lancamentos.LancarAsync(
                new NovoLancamentoModel { Tipo = "debito", Valor = Debito, Descricao = "Demo — débito" },
                Guid.NewGuid().ToString(), cancellationToken);
            console.WriteLine($"    lançamento {debito.Id} registrado");

            console.WriteLine("3/6 Aguardando a projeção do consolidado (outbox → Kafka → consumidor)...");
            var consolidado = await EsperarConsolidadoAsync(Credito - Debito, cancellationToken);

            console.WriteLine(
                $"4/6 Consolidado do dia: saldo do dia {Formatacao.Moeda(consolidado.SaldoDia)}, " +
                $"anterior {Formatacao.Moeda(consolidado.SaldoAnterior)}, " +
                $"acumulado {Formatacao.Moeda(consolidado.SaldoAcumulado)}");

            console.WriteLine("5/6 Estornando o crédito...");
            var chave = Guid.NewGuid().ToString();
            var estorno = await sessao.Lancamentos.EstornarAsync(credito.Id, chave, cancellationToken);
            console.WriteLine($"    estorno {estorno.Id} criado");

            console.WriteLine("6/6 Reenviando o estorno com a mesma chave (idempotência)...");
            var repetido = await sessao.Lancamentos.EstornarAsync(credito.Id, chave, cancellationToken);
            console.WriteLine(repetido.Id == estorno.Id
                ? "    mesma resposta: idempotência confirmada"
                : "    divergência: a API criou outro lançamento");

            console.WriteLine("Demo concluída.");
        }
        catch (ApiException ex)
        {
            console.WriteLine($"Demo interrompida: {ex.Message}");
        }
    }

    private async Task<ConsolidadoDiarioModel> EsperarConsolidadoAsync(decimal esperado, CancellationToken cancellationToken)
    {
        var hoje = new DiaDeNegocio(sessao.Relogio).Hoje();
        for (var tentativa = 1; ; tentativa++)
        {
            var atual = await sessao.Consolidado.ObterAsync(hoje, cancellationToken);
            if (atual.SaldoDia == esperado || tentativa >= 15)
                return atual;
            if (tentativa == 1)
                console.WriteLine("    ainda projetando...");
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }
    }
}
