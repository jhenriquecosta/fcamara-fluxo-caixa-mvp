using System.Net;
using Cashflow.Blazor.Modules.Core.Services;
using Cashflow.Blazor.Modules.Lancamentos.Models;
using Cashflow.Blazor.Modules.Lancamentos.Services;
using Microsoft.AspNetCore.Components;

namespace Cashflow.Blazor.Modules.Lancamentos.Pages;

public partial class Add
{
    [Inject]
    public LancamentoService LancamentoService { get; set; } = default!;

    protected NovoLancamentoModel Lancamento { get; private set; } = new();

    protected string? Sucesso { get; set; }

    /// <summary>
    /// A chave nasce com o formulário. Duplo clique ou falha de rede reenviam a mesma chave e a API
    /// devolve o mesmo lançamento (ADR-0004); uma recusa definitiva troca a chave, porque o próximo
    /// envio já é outra intenção, possivelmente com outro corpo.
    /// </summary>
    private string _idempotencyKey = Guid.NewGuid().ToString();

    protected async Task SalvarAsync()
    {
        Sucesso = null;
        await ExecutarAsync(async () =>
        {
            try
            {
                var criado = await LancamentoService.LancarAsync(Lancamento, _idempotencyKey);
                Sucesso = $"{Core.Formatacao.Tipo(criado.Tipo)} de {Core.Formatacao.Moeda(criado.Valor)} registrado em {Core.Formatacao.Dia(criado.Data)}.";
                Lancamento = new NovoLancamentoModel { Tipo = criado.Tipo };
                _idempotencyKey = Guid.NewGuid().ToString();
            }
            catch (ApiException ex) when (ex.StatusCode is not (HttpStatusCode.ServiceUnavailable or HttpStatusCode.Conflict))
            {
                _idempotencyKey = Guid.NewGuid().ToString();
                throw;
            }
        });
    }
}
