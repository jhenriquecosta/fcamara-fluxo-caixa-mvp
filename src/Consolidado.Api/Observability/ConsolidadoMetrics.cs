using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.Metrics;

namespace Cashflow.Consolidado.Api.Observability;

public sealed class ConsolidadoMetrics
{
    public const string MeterName = "Cashflow.Consolidado";

    private readonly Gauge<double> _lag;
    private readonly Counter<long> _dlt;

    public ConsolidadoMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _lag = meter.CreateGauge<double>("cashflow.consumer.lag",
            unit: "{message}", description: "Lag do consumidor");
        _dlt = meter.CreateCounter<long>("cashflow.dlt.events",
            description: "Eventos enviados à DLT");
    }

    public void Lag(double mensagens) => _lag.Record(mensagens);
    public void Dlt() => _dlt.Add(1);
}
