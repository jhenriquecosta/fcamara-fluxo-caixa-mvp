namespace Cashflow.Modules.Lancamentos.Models;

/// <summary>
/// Como terminou um registro ou estorno. O caso de uso devolve um destes, e o endpoint o traduz em
/// status HTTP: a regra não conhece HTTP, e o HTTP não decide regra.
/// </summary>
public abstract record ResultadoDoLancamento
{
    /// <summary>O lançamento (ou estorno) foi gravado agora. 201.</summary>
    public sealed record Criado(LancamentoModel Lancamento) : ResultadoDoLancamento;

    /// <summary>A chave já tinha sido concluída com o mesmo pedido: devolve a resposta gravada, sem gravar de novo. 201.</summary>
    public sealed record RespostaRepetida(string RespostaGravada) : ResultadoDoLancamento;

    /// <summary>A chave já foi usada com outro pedido. 422.</summary>
    public sealed record ChaveUsadaComOutroPedido : ResultadoDoLancamento;

    /// <summary>Outra requisição com a mesma chave ainda está em andamento. 409.</summary>
    public sealed record ChaveEmProcessamento : ResultadoDoLancamento;

    /// <summary>O lançamento a estornar não existe para este comerciante. 404.</summary>
    public sealed record LancamentoNaoEncontrado : ResultadoDoLancamento;

    /// <summary>O lançamento a estornar já é um estorno. 422.</summary>
    public sealed record EstornoDeEstorno : ResultadoDoLancamento;

    /// <summary>O lançamento já foi estornado antes. 409.</summary>
    public sealed record JaEstornado : ResultadoDoLancamento;
}
