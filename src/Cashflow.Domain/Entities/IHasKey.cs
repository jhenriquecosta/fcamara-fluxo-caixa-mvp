namespace Cashflow.Domain.Entities;

public interface IHasKey<TKey>
{
    TKey Id { get; set; }
}

public interface ITrackable
{
    DateTimeOffset CreatedDateTime { get; set; }
    DateTimeOffset? UpdatedDateTime { get; set; }
}
