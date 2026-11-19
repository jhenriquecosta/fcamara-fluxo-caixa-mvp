namespace Cashflow.Blazor.Modules.Lancamentos.Models;

/// <summary>A resposta 201 de lançar e de estornar.</summary>
public sealed record LancamentoCriadoModel(
    Guid Id, Guid ComercianteId, DateOnly Data, decimal Valor, string Tipo, string? Descricao, Guid? EstornoDe);
