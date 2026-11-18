using Cashflow.Domain.Entities;

namespace Cashflow.Modules.Lancamentos.Entities;

/// <summary>
/// O lançamento é imutável: nasce por <see cref="Registrar"/>, e a correção é um contra-lançamento
/// criado por <see cref="Estornar"/>. As regras vivem aqui, não no handler nem no SQL.
/// </summary>
public sealed class Lancamento : Entity<Guid>, IAggregateRoot
{
    private Lancamento()
    {
    }

    public Guid ComercianteId { get; private set; }

    public TipoLancamento Tipo { get; private set; }

    public decimal Valor { get; private set; }

    public string? Descricao { get; private set; }

    /// <summary>O dia de negócio (America/Sao_Paulo), não o dia UTC do servidor.</summary>
    public DateOnly Data { get; private set; }

    public Guid? EstornoDe { get; private set; }

    public bool EhEstorno => EstornoDe is not null;

    public static Lancamento Registrar(
        Guid comercianteId, TipoLancamento tipo, decimal valor, string? descricao,
        DateOnly data, DateTimeOffset agora)
    {
        if (valor <= 0)
            throw new ArgumentOutOfRangeException(nameof(valor), "valor deve ser maior que zero");

        return new Lancamento
        {
            Id = Guid.NewGuid(),
            ComercianteId = comercianteId,
            Tipo = tipo,
            Valor = valor,
            Descricao = descricao,
            Data = data,
            CreatedDateTime = agora
        };
    }

    public static Lancamento Estornar(Lancamento original, DateOnly data, DateTimeOffset agora)
    {
        if (original.EhEstorno)
            throw new LancamentoNaoPodeSerEstornadoException();

        return new Lancamento
        {
            Id = Guid.NewGuid(),
            ComercianteId = original.ComercianteId,
            Tipo = original.Tipo.Oposto(),
            Valor = original.Valor,
            Data = data,
            CreatedDateTime = agora,
            EstornoDe = original.Id
        };
    }

    /// <summary>Reconstrói um lançamento já gravado (uso do repositório).</summary>
    internal static Lancamento Restaurar(
        Guid id, Guid comercianteId, TipoLancamento tipo, decimal valor, string? descricao,
        DateOnly data, DateTimeOffset criadoEm, Guid? estornoDe) =>
        new()
        {
            Id = id,
            ComercianteId = comercianteId,
            Tipo = tipo,
            Valor = valor,
            Descricao = descricao,
            Data = data,
            CreatedDateTime = criadoEm,
            EstornoDe = estornoDe
        };
}
