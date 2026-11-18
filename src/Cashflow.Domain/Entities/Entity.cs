using System.ComponentModel.DataAnnotations;

namespace Cashflow.Domain.Entities;

public abstract class Entity<TKey> : IHasKey<TKey>, ITrackable
{
    public TKey Id { get; set; } = default!;

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public DateTimeOffset CreatedDateTime { get; set; }

    public DateTimeOffset? UpdatedDateTime { get; set; }
}
