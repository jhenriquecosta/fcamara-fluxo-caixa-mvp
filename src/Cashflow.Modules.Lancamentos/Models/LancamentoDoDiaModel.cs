namespace Cashflow.Modules.Lancamentos.Models;

/// <summary>Um lançamento na lista do dia, com o que a tela precisa para decidir se ele ainda pode ser estornado.</summary>
public sealed record LancamentoDoDiaModel(
    Guid Id, DateOnly Data, decimal Valor, string Tipo, string? Descricao,
    Guid? EstornoDe, bool Estornado, DateTimeOffset CriadoEm);
