namespace Cashflow.Domain.Entities;

public interface ITrackable
{
    DateTimeOffset CreatedDateTime { get; set; }

    DateTimeOffset? UpdatedDateTime { get; set; }
}
