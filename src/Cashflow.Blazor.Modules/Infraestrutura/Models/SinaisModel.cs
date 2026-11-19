namespace Cashflow.Blazor.Modules.Infraestrutura.Models;

/// <summary>
/// Os sinais que as regras de alerta vigiam (<c>observability/prometheus/rules</c>), lidos do
/// Prometheus no instante da consulta.
/// </summary>
/// <param name="Servicos">Os sinais dourados por serviço.</param>
/// <param name="OutboxIdadeSegundos">A idade do evento mais antigo não publicado.</param>
/// <param name="ConsumerLag">Quantas mensagens o consumidor do consolidado tem por ler.</param>
/// <param name="DltUltimaHora">Eventos enviados à dead-letter na última hora.</param>
/// <param name="LancamentosPorResultado">Contagem de lançamentos por desfecho (registrado, idempotente, ...).</param>
public sealed record SinaisModel(
    IReadOnlyList<SinalServico> Servicos,
    double? OutboxIdadeSegundos,
    double? ConsumerLag,
    double DltUltimaHora,
    IReadOnlyDictionary<string, double> LancamentosPorResultado);

/// <summary>Os sinais dourados de um serviço.</summary>
/// <param name="Servico">O rótulo <c>service</c>.</param>
/// <param name="Up">O Prometheus consegue coletar o serviço.</param>
/// <param name="RequisicoesPorSegundo">Tráfego de negócio (sem <c>/metrics</c> e <c>/health</c>) no último minuto.</param>
/// <param name="P95Ms">Latência p95 em milissegundos, nos últimos 5 minutos.</param>
/// <param name="TaxaErro">Fração de respostas 5xx nos últimos 5 minutos.</param>
public sealed record SinalServico(
    string Servico, bool Up, double RequisicoesPorSegundo, double? P95Ms, double? TaxaErro);
