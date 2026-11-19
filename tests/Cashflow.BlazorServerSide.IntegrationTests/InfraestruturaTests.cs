using System.Net;
using Cashflow.Blazor.Modules.Infraestrutura;
using Cashflow.Blazor.Modules.Infraestrutura.Services;
using Microsoft.Extensions.Options;

namespace Cashflow.BlazorServerSide.IntegrationTests;

/// <summary>PRD-0004 RF6/RF7: os clientes de infraestrutura leem as fontes reais e degradam sozinhos.</summary>
public sealed class InfraestruturaTests
{
    private static HttpClient Cliente(ApiFalsa api, string baseUrl = "http://fonte/") =>
        new(api) { BaseAddress = new Uri(baseUrl) };

    private static string Vetor(params (string Rotulo, string Valor, string Numero)[] series)
    {
        var resultado = string.Join(",", series.Select(s =>
            "{\"metric\":{\"" + s.Rotulo + "\":\"" + s.Valor + "\"},\"value\":[1700000000,\"" + s.Numero + "\"]}"));
        return "{\"status\":\"success\",\"data\":{\"resultType\":\"vector\",\"result\":[" + resultado + "]}}";
    }

    [Fact]
    [Trait("Criterio", "AC3")]
    public async Task Os_sinais_do_painel_vem_das_mesmas_series_que_os_alertas_vigiam()
    {
        // Arrange
        var api = new ApiFalsa(request =>
        {
            var q = Uri.UnescapeDataString(request.RequestUri!.Query);
            return ApiFalsa.Json(q switch
            {
                _ when q.Contains("up{") => Vetor(("job", "lancamentos", "1"), ("job", "consolidado", "0"), ("job", "background-server", "1")),
                _ when q.Contains("histogram_quantile") => Vetor(("service", "lancamentos", "0.042")),
                _ when q.Contains("5..") => Vetor(("service", "lancamentos", "0.01")),
                _ when q.Contains("rate(http_server_request_duration_seconds_count") => Vetor(("service", "lancamentos", "2.5")),
                _ when q.Contains("outbox") => Vetor(("x", "y", "0.8")),
                _ when q.Contains("consumer_lag") => Vetor(("x", "y", "3")),
                _ when q.Contains("dlt") => Vetor(),
                _ when q.Contains("cashflow_lancamentos_total") => Vetor(("resultado", "registrado", "7"), ("resultado", "idempotente", "2")),
                _ => Vetor()
            });
        });
        var prometheus = new PrometheusService(Cliente(api));

        // Act
        var sinais = await prometheus.ObterSinaisAsync(TestContext.Current.CancellationToken);

        // Assert
        var lancamentos = sinais.Servicos.Single(s => s.Servico == "lancamentos");
        Assert.True(lancamentos.Up);
        Assert.Equal(2.5, lancamentos.RequisicoesPorSegundo);
        Assert.Equal(42, lancamentos.P95Ms!.Value, 3);
        Assert.Equal(0.01, lancamentos.TaxaErro);
        Assert.False(sinais.Servicos.Single(s => s.Servico == "consolidado").Up);
        Assert.Equal(0.8, sinais.OutboxIdadeSegundos);
        Assert.Equal(3, sinais.ConsumerLag);
        Assert.Equal(0, sinais.DltUltimaHora);
        Assert.Equal(9, sinais.LancamentosPorResultado.Values.Sum());
        Assert.All(api.Recebidas.Where(r => r.RequestUri!.Query.Contains("http_server")),
            r => Assert.Contains("/metrics|/health", Uri.UnescapeDataString(r.RequestUri!.Query)));
    }

    [Fact]
    [Trait("Criterio", "AC3")]
    public async Task Os_logs_vem_do_indice_do_serilog_sem_o_ruido_de_metrics_e_health()
    {
        // Arrange
        var api = new ApiFalsa(_ => ApiFalsa.Json("""
            {"hits":{"hits":[
              {"_source":{"@timestamp":"2026-11-19T14:56:33.56+00:00","level":"Warning","message":"idempotência com payload divergente",
                          "fields":{"service":"lancamentos","SourceContext":"Cashflow.Modules.Lancamentos.Commands.RegistrarLancamentoCommandHandler"}}}
            ]}}
            """));
        var logs = new LogsService(Cliente(api));

        // Act
        var entradas = await logs.UltimosAsync(10, "lancamentos", "Warning", TestContext.Current.CancellationToken);

        // Assert
        var entrada = Assert.Single(entradas);
        Assert.Equal("Warning", entrada.Nivel);
        Assert.Equal("lancamentos", entrada.Servico);
        Assert.Contains("payload divergente", entrada.Mensagem);
        var consulta = Assert.Single(api.Corpos)!;
        Assert.Contains("\"/metrics\"", consulta);
        Assert.Contains("\"/health\"", consulta);
        Assert.Contains("\"fields.service\":\"lancamentos\"", consulta);
        Assert.Contains("[\"Warning\",\"Error\",\"Fatal\"]", consulta);
        Assert.StartsWith("/cashflow-logs-*/_search", api.Recebidas.Single().RequestUri!.PathAndQuery);
    }

    [Fact]
    [Trait("Criterio", "AC5")]
    public async Task Um_servico_fora_aparece_fora_sem_atrasar_nem_derrubar_os_outros()
    {
        // Arrange
        var api = new ApiFalsa(request => request.RequestUri!.Host == "fora"
            ? throw new HttpRequestException("Connection refused")
            : ApiFalsa.Json("""{"status":"ok"}"""));
        var saude = new SaudeService(new HttpClient(api), Options.Create(new InfraestruturaOptions
        {
            Servicos =
            [
                new ServicoMonitorado { Nome = "lancamentos-api", HealthUrl = "http://de-pe/health" },
                new ServicoMonitorado { Nome = "consolidado-api", HealthUrl = "http://fora/health" }
            ]
        }));

        // Act
        var resultado = await saude.ChecarAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Collection(resultado,
            s => Assert.True(s.Disponivel),
            s =>
            {
                Assert.False(s.Disponivel);
                Assert.Contains("Connection refused", s.Erro);
            });
    }

    [Fact]
    [Trait("Criterio", "AC5")]
    public async Task O_prometheus_recusando_a_consulta_vira_erro_explicito()
    {
        // Arrange
        var api = new ApiFalsa(_ => ApiFalsa.Json("""{"status":"error","errorType":"bad_data"}""", HttpStatusCode.OK));
        var prometheus = new PrometheusService(Cliente(api));

        // Act
        var erro = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            prometheus.ConsultarAsync("up", TestContext.Current.CancellationToken));

        // Assert
        Assert.Contains("recusou", erro.Message);
    }
}
