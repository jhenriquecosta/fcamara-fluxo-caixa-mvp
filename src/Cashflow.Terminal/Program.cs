// O ponto de entrada do terminal (PRD-0004 RF4/RF5): escolhe o perfil, obtém o token de
// desenvolvimento e entrega a sessão ao menu — ou roda a demo narrada, sem menu.
using Cashflow.Blazor.Modules.Consolidado.Services;
using Cashflow.Blazor.Modules.Core.Authorization;
using Cashflow.Blazor.Modules.Infraestrutura;
using Cashflow.Blazor.Modules.Infraestrutura.Services;
using Cashflow.Blazor.Modules.Lancamentos.Services;
using Cashflow.Terminal;
using Microsoft.Extensions.Options;
using Spectre.Console;

var console = AnsiConsole.Console;

string Env(string nome, string padrao) =>
    Environment.GetEnvironmentVariable(nome) is { Length: > 0 } valor ? valor : padrao;

var lancamentosUrl = Env("CASHFLOW_LANCAMENTOS_URL", "http://localhost:8080");
var consolidadoUrl = Env("CASHFLOW_CONSOLIDADO_URL", "http://localhost:8081");
var backgroundUrl = Env("CASHFLOW_BACKGROUND_URL", "http://localhost:8082");
var prometheusUrl = Env("CASHFLOW_PROMETHEUS_URL", "http://localhost:9090");
var elasticsearchUrl = Env("CASHFLOW_ELASTICSEARCH_URL", "http://localhost:9200");
var comerciante = Guid.Parse(Env("CASHFLOW_COMERCIANTE_ID", "7c9e6679-7425-40de-944b-e07fc1f90ae7"));

var comando = args.FirstOrDefault();
var perfil = comando == "demo" ? Perfis.Operador : SelecaoDePerfil.Escolher(console);

HttpClient Cliente(string url, TimeSpan? prazo = null)
{
    var cliente = new HttpClient { BaseAddress = new Uri(url.EndsWith('/') ? url : url + "/") };
    if (prazo is { } t)
        cliente.Timeout = t;
    return cliente;
}

using var tokenHttp = Cliente(lancamentosUrl);
using var lancamentosHttp = Cliente(lancamentosUrl);
using var consolidadoHttp = Cliente(consolidadoUrl);
using var saudeHttp = Cliente(lancamentosUrl);
using var prometheusHttp = Cliente(prometheusUrl, TimeSpan.FromSeconds(5));
using var elasticsearchHttp = Cliente(elasticsearchUrl, TimeSpan.FromSeconds(5));

var token = new DevTokenManager(tokenHttp, comerciante, EscoposDoContrato.DoPerfil(perfil));
var infraestrutura = new InfraestruturaOptions
{
    Enabled = true,
    PrometheusUrl = prometheusUrl,
    ElasticsearchUrl = elasticsearchUrl,
    Servicos =
    [
        new ServicoMonitorado { Nome = "lancamentos", Papel = "API de Lançamentos", HealthUrl = lancamentosUrl + "/health" },
        new ServicoMonitorado { Nome = "consolidado", Papel = "API de Consolidado", HealthUrl = consolidadoUrl + "/health" },
        new ServicoMonitorado { Nome = "background-server", Papel = "Outbox e consumidor do consolidado", HealthUrl = backgroundUrl + "/health" },
        new ServicoMonitorado { Nome = "prometheus", Papel = "Métricas", HealthUrl = prometheusUrl + "/-/healthy" },
        new ServicoMonitorado { Nome = "elasticsearch", Papel = "Logs", HealthUrl = elasticsearchUrl + "/_cluster/health" }
    ]
};

var sessao = new Sessao
{
    Perfil = perfil,
    Lancamentos = new LancamentoService(lancamentosHttp, token),
    Consolidado = new ConsolidadoService(consolidadoHttp, token),
    Saude = new SaudeService(saudeHttp, Options.Create(infraestrutura)),
    Prometheus = new PrometheusService(prometheusHttp),
    Logs = new LogsService(elasticsearchHttp),
    Relogio = TimeProvider.System
};

if (comando == "demo")
    await new TelaDemo(console, sessao).ExecutarAsync();
else
    await new MenuPrincipal(console, sessao).ExecutarAsync();
