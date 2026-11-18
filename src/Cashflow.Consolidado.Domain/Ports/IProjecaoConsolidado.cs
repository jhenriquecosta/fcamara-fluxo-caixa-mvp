namespace Cashflow.Consolidado.Domain.Ports;

public enum ProjecaoResultado
{
    Aplicado,
    Duplicado
}

public interface IProjecaoConsolidado
{
    Task<ProjecaoResultado> AplicarAsync(LancamentoEvento evento, CancellationToken ct);
}
