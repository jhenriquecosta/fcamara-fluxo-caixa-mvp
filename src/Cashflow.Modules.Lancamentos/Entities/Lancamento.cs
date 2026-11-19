using Cashflow.Domain.Entities;

namespace Cashflow.Modules.Lancamentos.Entities;

/// <summary>
/// Um crédito ou débito no caixa de um comerciante: a raiz do agregado do módulo.
/// </summary>
/// <remarks>
/// O lançamento é imutável, como num livro-caixa: nasce por <see cref="Registrar"/> e nunca é editado
/// nem apagado. Um erro se corrige com um contra-lançamento de tipo oposto, criado por
/// <see cref="Estornar"/>, e os dois ficam na história. As regras vivem aqui, não no handler nem no SQL.
/// </remarks>
public sealed class Lancamento : Entity<Guid>, IAggregateRoot
{
    private Lancamento()
    {
    }

    /// <summary>O dono do caixa. Vem do token de quem registrou, nunca do corpo da requisição.</summary>
    public Guid ComercianteId { get; private set; }

    /// <summary>Se o lançamento entra (crédito) ou sai (débito) do caixa.</summary>
    public TipoLancamento Tipo { get; private set; }

    /// <summary>O valor, sempre positivo. O sinal no saldo vem do <see cref="Tipo"/>.</summary>
    public decimal Valor { get; private set; }

    /// <summary>Texto livre do comerciante. O estorno não tem: ele se explica pelo <see cref="EstornoDe"/>.</summary>
    public string? Descricao { get; private set; }

    /// <summary>O dia de negócio (America/Sao_Paulo), não o dia UTC do servidor. É o dia em que o lançamento conta no consolidado.</summary>
    public DateOnly Data { get; private set; }

    /// <summary>O lançamento que este estorna, ou <c>null</c> num lançamento comum.</summary>
    public Guid? EstornoDe { get; private set; }

    /// <summary>Diz se este lançamento é um estorno de outro.</summary>
    public bool EhEstorno => EstornoDe is not null;

    /// <summary>Registra um lançamento novo no caixa do comerciante.</summary>
    /// <param name="comercianteId">O dono do caixa.</param>
    /// <param name="tipo">Crédito ou débito.</param>
    /// <param name="valor">O valor; precisa ser maior que zero.</param>
    /// <param name="descricao">Texto livre, opcional.</param>
    /// <param name="data">O dia de negócio, já calculado no fuso do negócio.</param>
    /// <param name="agora">O instante do registro.</param>
    /// <exception cref="ArgumentOutOfRangeException">O valor é zero ou negativo.</exception>
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

    /// <summary>
    /// Cria o contra-lançamento que anula <paramref name="original"/> no saldo: mesmo valor, tipo oposto,
    /// lançado no dia de hoje. O original não muda.
    /// </summary>
    /// <param name="original">O lançamento a corrigir.</param>
    /// <param name="data">O dia de negócio do estorno, que pode ser diferente do dia do original.</param>
    /// <param name="agora">O instante do estorno.</param>
    /// <exception cref="LancamentoNaoPodeSerEstornadoException">O original já é um estorno.</exception>
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

    /// <summary>
    /// Remonta um lançamento lido do banco, sem passar pelas regras de criação: elas já valeram
    /// quando ele foi gravado. Só o repositório usa.
    /// </summary>
    internal static Lancamento ReconstituirDoBanco(
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
