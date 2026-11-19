namespace Cashflow.Domain.Entities;

/// <summary>Uma entidade que sabe quando nasceu. O instante vem do relógio da aplicação, não do banco.</summary>
public interface ITrackable
{
    /// <summary>O instante de criação, em UTC.</summary>
    DateTimeOffset CreatedDateTime { get; set; }
}
