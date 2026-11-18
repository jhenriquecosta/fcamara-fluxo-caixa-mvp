namespace Cashflow.Modules.Lancamentos.Entities;

/// <summary>
/// O lançamento é imutável: nasce por <see cref="Registrar"/> e a correção é um
/// contra-lançamento. As regras do estorno vivem aqui, não na infraestrutura.
/// </summary>
public sealed record Lancamento(
    Guid Id,
    Guid ComercianteId,
    TipoLancamento Tipo,
    decimal Valor,
    string? Descricao,
    DateOnly Data,
    DateTimeOffset CriadoEm,
    Guid? EstornoDe)
{
    public static Lancamento Registrar(
        Guid comercianteId, TipoLancamento tipo, decimal valor, string? descricao,
        DateOnly data, DateTimeOffset agora)
    {
        if (valor <= 0)
            throw new ArgumentOutOfRangeException(nameof(valor), "valor deve ser maior que zero");

        return new Lancamento(Guid.NewGuid(), comercianteId, tipo, valor, descricao, data, agora, null);
    }

    public static Lancamento Estornar(Lancamento original, DateOnly data, DateTimeOffset agora)
    {
        if (original.EstornoDe is not null)
            throw new LancamentoNaoPodeSerEstornadoException();

        return new Lancamento(
            Guid.NewGuid(), original.ComercianteId, original.Tipo.Oposto(),
            original.Valor, null, data, agora, original.Id);
    }
}
