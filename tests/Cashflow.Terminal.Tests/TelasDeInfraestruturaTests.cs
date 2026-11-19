using Cashflow.Blazor.Modules.Core.Authorization;
using Cashflow.Blazor.Modules.Infraestrutura;
using Spectre.Console.Testing;

namespace Cashflow.Terminal.Tests;

/// <summary>PRD-0004 RF5/RF7/AC5: as telas de infraestrutura mostram os dados reais e degradam sozinhas.</summary>
public sealed class TelasDeInfraestruturaTests
{
    private static InfraestruturaOptions DoisServicos() => new()
    {
        Servicos =
        [
            new ServicoMonitorado { Nome = "lancamentos", Papel = "API", HealthUrl = "http://api/health/lancamentos" },
            new ServicoMonitorado { Nome = "consolidado", Papel = "API", HealthUrl = "http://api/health/consolidado" }
        ]
    };

    private static string Vetor(params (string Rotulo, string Valor, string Numero)[] series) =>
        "{\"status\":\"success\",\"data\":{\"resultType\":\"vector\",\"result\":[" +
        string.Join(",", series.Select(s =>
            "{\"metric\":{\"" + s.Rotulo + "\":\"" + s.Valor + "\"},\"value\":[1700000000,\"" + s.Numero + "\"]}")) +
        "]}}";

    [Fact]
    [Trait("Criterio", "AC5")]
    public async Task Saude_mostra_quem_esta_no_ar_e_quem_esta_fora()
    {
        var api = new ApiFalsa(request => request.RequestUri!.AbsolutePath.Contains("lancamentos")
            ? ApiFalsa.Json("{}")
            : ApiFalsa.Json("{}", 503));
        var console = new TestConsole();

        await new TelaSaude(console, SessaoDeTeste.Para(Perfis.Consulta, api, DoisServicos()))
            .ExecutarAsync(TestContext.Current.CancellationToken);

        Assert.Contains("no ar", console.Output);
        Assert.Contains("fora", console.Output);
    }

    [Fact]
    [Trait("Criterio", "AC5")]
    public async Task Saude_com_a_fonte_fora_mostra_tudo_fora_sem_derrubar()
    {
        var api = new ApiFalsa(_ => throw new HttpRequestException("conexão recusada"));
        var console = new TestConsole();

        await new TelaSaude(console, SessaoDeTeste.Para(Perfis.Consulta, api, DoisServicos()))
            .ExecutarAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, console.Output.Split("fora").Length - 1);
    }

    [Fact]
    [Trait("Criterio", "AC4")]
    public async Task Metricas_mostram_os_sinais_dourados()
    {
        var api = new ApiFalsa(request =>
        {
            var q = Uri.UnescapeDataString(request.RequestUri!.Query);
            return ApiFalsa.Json(q switch
            {
                _ when q.Contains("up{") => Vetor(("job", "lancamentos", "1")),
                _ when q.Contains("histogram_quantile") => Vetor(("service", "lancamentos", "0.042")),
                _ when q.Contains("5..") => Vetor(("service", "lancamentos", "0.01")),
                _ when q.Contains("rate(http_server_request_duration_seconds_count") => Vetor(("service", "lancamentos", "2.5")),
                _ => Vetor()
            });
        });
        var console = new TestConsole();

        await new TelaMetricas(console, SessaoDeTeste.Para(Perfis.Consulta, api))
            .ExecutarAsync(TestContext.Current.CancellationToken);

        Assert.Contains("lancamentos", console.Output);
        Assert.Contains("up", console.Output);
        Assert.Contains("42", console.Output);
    }

    [Fact]
    [Trait("Criterio", "AC5")]
    public async Task Metricas_com_o_prometheus_fora_aparecem_indisponiveis()
    {
        var api = new ApiFalsa(_ => throw new HttpRequestException("conexão recusada"));
        var console = new TestConsole();

        await new TelaMetricas(console, SessaoDeTeste.Para(Perfis.Consulta, api))
            .ExecutarAsync(TestContext.Current.CancellationToken);

        Assert.Contains("indispon", console.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Criterio", "AC4")]
    public async Task Logs_mostram_as_ultimas_entradas()
    {
        var api = new ApiFalsa(_ => ApiFalsa.Json(
            """{"hits":{"hits":[{"_source":{"@timestamp":"2026-11-19T12:00:00Z","level":"Information","message":"lançamento registrado","fields":{"service":"lancamentos","SourceContext":"Handler"}}}]}}"""));
        var console = new TestConsole();

        await new TelaLogs(console, SessaoDeTeste.Para(Perfis.Consulta, api))
            .ExecutarAsync(TestContext.Current.CancellationToken);

        Assert.Contains("lançamento registrado", console.Output);
        Assert.Contains("lancamentos", console.Output);
    }

    [Fact]
    [Trait("Criterio", "AC5")]
    public async Task Logs_com_o_elasticsearch_fora_aparecem_indisponiveis()
    {
        var api = new ApiFalsa(_ => throw new HttpRequestException("conexão recusada"));
        var console = new TestConsole();

        await new TelaLogs(console, SessaoDeTeste.Para(Perfis.Consulta, api))
            .ExecutarAsync(TestContext.Current.CancellationToken);

        Assert.Contains("indispon", console.Output, StringComparison.OrdinalIgnoreCase);
    }
}
