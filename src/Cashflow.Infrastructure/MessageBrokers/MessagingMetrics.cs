using System.Diagnostics.Metrics;

namespace Cashflow.Infrastructure.MessageBrokers;

/// <summary>
/// As métricas do consumo, que alimentam os alertas de SLO (observability/prometheus/rules): o
/// lag diz se o consolidado está ficando para trás; a DLT diz se há evento que não consegue ser processado.
/// </summary>
public sealed class MessagingMetrics
{
    /// <summary>O nome do medidor, exportado por todo host que consome.</summary>
    public const string MeterName = "Cashflow.Messaging";

    private readonly Gauge<double> _consumerLag;
    private readonly Counter<long> _deadLetterEvents;

    /// <summary>Cria os instrumentos no medidor <see cref="MeterName"/>.</summary>
    public MessagingMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _consumerLag = meter.CreateGauge<double>("cashflow.consumer.lag",
            unit: "{message}", description: "Lag do consumidor");
        _deadLetterEvents = meter.CreateCounter<long>("cashflow.dlt.events",
            description: "Eventos enviados à DLT");
    }

    /// <summary>Quantas mensagens da partição ainda faltam consumir (<c>cashflow_consumer_lag</c>).</summary>
    public void RecordConsumerLag(double pendingMessages) => _consumerLag.Record(pendingMessages);

    /// <summary>Conta um evento desviado para a DLT (<c>cashflow_dlt_events_total</c>).</summary>
    public void CountDeadLetter() => _deadLetterEvents.Add(1);
}
