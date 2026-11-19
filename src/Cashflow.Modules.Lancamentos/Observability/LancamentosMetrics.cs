using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.Metrics;

namespace Cashflow.Modules.Lancamentos.Observability;

/// <summary>
/// As métricas de negócio do módulo, que o Grafana mostra e o Prometheus alerta: quantos lançamentos
/// por desfecho e há quanto tempo o evento mais antigo espera no outbox.
/// </summary>
public sealed class LancamentosMetrics
{
    /// <summary>O nome do medidor, que os hosts passam ao <c>AddCashflowInfrastructure</c>.</summary>
    public const string MeterName = "Cashflow.Lancamentos";

    private readonly Counter<long> _lancamentosPorDesfecho;
    private readonly Gauge<double> _idadeDoPendenteMaisAntigo;

    /// <summary>Cria os instrumentos no medidor <see cref="MeterName"/>.</summary>
    public LancamentosMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _lancamentosPorDesfecho = meter.CreateCounter<long>(
            "cashflow.lancamentos",
            description: "Lançamentos registrados por resultado");

        _idadeDoPendenteMaisAntigo = meter.CreateGauge<double>(
            "cashflow.outbox.pending_age_seconds",
            unit: "s",
            description: "Idade do evento mais antigo ainda não publicado");
    }

    /// <summary>Um lançamento novo foi gravado.</summary>
    public void Registrado() => Contar("registrado");

    /// <summary>Uma requisição repetida recebeu a resposta gravada.</summary>
    public void Idempotente() => Contar("idempotente");

    /// <summary>Uma chave foi reusada com outro pedido.</summary>
    public void ConflitoPayload() => Contar("conflito_payload");

    /// <summary>Duas requisições com a mesma chave chegaram juntas.</summary>
    public void ConflitoConcorrente() => Contar("conflito_concorrente");

    /// <summary>Um estorno foi gravado.</summary>
    public void Estornado() => Contar("estornado");

    /// <summary>
    /// Há quantos segundos o evento pendente mais antigo espera. Crescendo, o publicador parou ou o
    /// broker está fora, e o consolidado vai ficar para trás.
    /// </summary>
    public void RegistrarIdadeDoPendenteMaisAntigo(double segundos) => _idadeDoPendenteMaisAntigo.Record(segundos);

    private void Contar(string desfecho) =>
        _lancamentosPorDesfecho.Add(1, new KeyValuePair<string, object?>("resultado", desfecho));
}
