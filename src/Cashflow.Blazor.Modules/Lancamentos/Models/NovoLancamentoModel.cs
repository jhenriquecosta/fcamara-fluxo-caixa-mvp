using System.ComponentModel.DataAnnotations;

namespace Cashflow.Blazor.Modules.Lancamentos.Models;

/// <summary>
/// O formulário de lançamento. A validação aqui poupa uma ida à API; a regra que vale é a da API
/// (422 para valor ≤ 0 ou tipo inválido).
/// </summary>
public sealed class NovoLancamentoModel
{
    [Required(ErrorMessage = "Informe o valor.")]
    [Range(typeof(decimal), "0.01", "999999999.99", ErrorMessage = "O valor deve ser maior que zero.")]
    public decimal? Valor { get; set; }

    [Required(ErrorMessage = "Escolha crédito ou débito.")]
    [RegularExpression("^(credito|debito)$", ErrorMessage = "Escolha crédito ou débito.")]
    public string Tipo { get; set; } = "credito";

    [StringLength(200, ErrorMessage = "A descrição tem no máximo 200 caracteres.")]
    public string? Descricao { get; set; }
}
