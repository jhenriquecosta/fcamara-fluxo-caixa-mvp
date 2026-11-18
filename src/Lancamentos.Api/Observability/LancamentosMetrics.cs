using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.Metrics;

namespace Cashflow.Lancamentos.Api.Observability;

public sealed class LancamentosMetrics
{
    public const string MeterName = "Cashflow.Lancamentos";

    private readonly Counter<long> _total;
    private readonly Gauge<double> _outboxAge;

    public LancamentosMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _total = meter.CreateCounter<long>(
            "cashflow.lancamentos",
            description: "Lançamentos registrados por resultado");

        _outboxAge = meter.CreateGauge<double>(
            "cashflow.outbox.pending_age_seconds",
            unit: "s",
            description: "Idade do evento mais antigo ainda não publicado");
    }

    public void Registrado() => _total.Add(1, new KeyValuePair<string, object?>("resultado", "registrado"));
    public void Idempotente() => _total.Add(1, new KeyValuePair<string, object?>("resultado", "idempotente"));
    public void ConflitoPayload() => _total.Add(1, new KeyValuePair<string, object?>("resultado", "conflito_payload"));
    public void ConflitoConcorrente() => _total.Add(1, new KeyValuePair<string, object?>("resultado", "conflito_concorrente"));
    public void Estornado() => _total.Add(1, new KeyValuePair<string, object?>("resultado", "estornado"));
    public void OutboxAge(double segundos) => _outboxAge.Record(segundos);
}
