namespace Cashflow.Domain.Entities;

/// <summary>
/// A base das entidades persistidas: identidade e o instante de criação. Não há
/// <c>UpdatedDateTime</c> de propósito: no livro-caixa nada é alterado depois de gravado
/// (a correção é um estorno), e um campo de atualização sugeriria o contrário.
/// </summary>
/// <typeparam name="TKey">O tipo da identidade (aqui, sempre <see cref="Guid"/>).</typeparam>
public abstract class Entity<TKey> : ITrackable
{
    /// <summary>A identidade da entidade, gerada pela própria aplicação, nunca pelo banco.</summary>
    public TKey Id { get; set; } = default!;

    /// <inheritdoc />
    public DateTimeOffset CreatedDateTime { get; set; }
}
