using Cashflow.Domain.Repositories;
using Cashflow.Modules.Lancamentos.Entities;

namespace Cashflow.Modules.Lancamentos.Repositories;

/// <summary>O outbox do módulo: os eventos esperando para ir ao barramento.</summary>
public interface IOutboxEventRepository : IRepository<OutboxEvent, Guid>
{
    /// <summary>Os pendentes, em ordem, travados com SKIP LOCKED: dois publicadores não pegam o mesmo lote.</summary>
    Task<IReadOnlyList<OutboxEvent>> GetPendentesAsync(int limite, CancellationToken cancellationToken = default);

    /// <summary>Tira o evento da fila de pendentes, registrando quando foi publicado.</summary>
    Task MarcarPublicadoAsync(Guid id, DateTimeOffset publicadoEm, CancellationToken cancellationToken = default);

    /// <summary>Há quantos segundos o pendente mais antigo espera; zero com o outbox vazio.</summary>
    Task<double> IdadeDoPendenteMaisAntigoAsync(CancellationToken cancellationToken = default);
}
