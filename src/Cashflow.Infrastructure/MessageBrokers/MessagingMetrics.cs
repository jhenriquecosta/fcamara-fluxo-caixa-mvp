using System.Diagnostics.Metrics;

namespace Cashflow.Infrastructure.MessageBrokers;

public sealed class MessagingMetrics
{
    public const string MeterName = "Cashflow.Messaging";

    private readonly Gauge<double> _lag;
    private readonly Counter<long> _dlt;

    public MessagingMetrics(IMeterFactory meterFactory)
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
