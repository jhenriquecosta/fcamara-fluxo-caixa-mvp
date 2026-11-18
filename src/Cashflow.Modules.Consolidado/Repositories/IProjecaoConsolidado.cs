using Cashflow.Modules.Consolidado.Entities;

namespace Cashflow.Modules.Consolidado.Repositories;

public enum ProjecaoResultado
{
    Aplicado,
    Duplicado
}

public interface IProjecaoConsolidado
{
    Task<ProjecaoResultado> AplicarAsync(LancamentoEvento evento, CancellationToken ct);
}
