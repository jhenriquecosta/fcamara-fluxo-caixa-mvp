using System.Globalization;
using Cashflow.Blazor.Modules.Core.Services;
using Cashflow.Blazor.Modules.Lancamentos.Models;

namespace Cashflow.Blazor.Modules.Lancamentos.Services;

/// <summary>O cliente do serviço de Lançamentos (o <c>ProductService</c> da referência).</summary>
public class LancamentoService(HttpClient httpClient, ITokenManager tokenManager) : HttpService(httpClient, tokenManager)
{
    /// <summary>Os lançamentos do dia (nulo: hoje em São Paulo), com a marca de estornado.</summary>
    public Task<List<LancamentoModel>> ListarAsync(DateOnly? data, CancellationToken cancellationToken = default) =>
        GetAsync<List<LancamentoModel>>(data is { } dia
            ? $"api/v1/lancamentos?data={dia.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}"
            : "api/v1/lancamentos", cancellationToken);

    /// <summary>Registra o lançamento; a mesma chave devolve o mesmo resultado.</summary>
    public Task<LancamentoCriadoModel> LancarAsync(NovoLancamentoModel lancamento, string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync<LancamentoCriadoModel>("api/v1/lancamentos",
            new { valor = lancamento.Valor, tipo = lancamento.Tipo, descricao = Vazio(lancamento.Descricao) },
            idempotencyKey, cancellationToken);

    /// <summary>Cria o lançamento inverso; o original não muda.</summary>
    public Task<LancamentoCriadoModel> EstornarAsync(Guid lancamentoId, string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync<LancamentoCriadoModel>($"api/v1/lancamentos/{lancamentoId}/estornos", null,
            idempotencyKey, cancellationToken);

    private static string? Vazio(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
