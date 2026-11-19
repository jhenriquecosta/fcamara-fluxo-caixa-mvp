using Cashflow.Domain.Repositories;
using Cashflow.Modules.Lancamentos.Entities;

namespace Cashflow.Modules.Lancamentos.Repositories;

public interface IOutboxEventRepository : IRepository<OutboxEvent, Guid>
{
    /// <summary>Os pendentes, em ordem, travados com SKIP LOCKED: dois publicadores não pegam o mesmo lote.</summary>
    Task<IReadOnlyList<OutboxEvent>> GetPendentesAsync(int limite, CancellationToken cancellationToken = default);

    Task MarcarPublicadoAsync(Guid id, DateTimeOffset publicadoEm, CancellationToken cancellationToken = default);

    Task<double> IdadeDoPendenteMaisAntigoAsync(CancellationToken cancellationToken = default);
}
