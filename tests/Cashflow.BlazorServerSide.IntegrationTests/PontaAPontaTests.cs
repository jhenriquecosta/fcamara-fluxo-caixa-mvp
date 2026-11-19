using System.Net;
using Cashflow.Blazor.Modules.Consolidado.Services;
using Cashflow.Blazor.Modules.Core.Services;
using Cashflow.Blazor.Modules.Lancamentos.Models;
using Cashflow.Blazor.Modules.Lancamentos.Services;
using Cashflow.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Cashflow.BlazorServerSide.IntegrationTests;

/// <summary>
/// O frontend contra a API real em memória (os dois módulos e o PostgreSQL do Testcontainers):
/// os serviços que as telas chamam, com o token que o host emite — critérios 2, 3, 4 e 5.
/// </summary>
[Collection("integracao")]
public sealed class PontaAPontaTests(PostgresFixture db)
{
    [Fact]
    [Trait("Criterio", "AC3")]
    [Trait("Criterio", "AC4")]
    public async Task Lancado_pela_tela_aparece_na_lista_e_o_estorno_marca_o_original()
    {
        await using var api = new WebApiFactory(db.ConnectionString);
        await using var front = new FrontendFactory(() => api.Server.CreateHandler());
        await using var sessao = front.Sessao(FrontendFactory.Operador);
        var lancamentos = sessao.ServiceProvider.GetRequiredService<LancamentoService>();
        var ct = TestContext.Current.CancellationToken;

        var venda = await lancamentos.LancarAsync(
            new NovoLancamentoModel { Valor = 250.00m, Tipo = "credito", Descricao = "venda balcão" },
            Guid.NewGuid().ToString(), ct);
        var repetida = await lancamentos.LancarAsync(
            new NovoLancamentoModel { Valor = 250.00m, Tipo = "credito", Descricao = "venda balcão" },
            "mesma-chave", ct);
        var reenvio = await lancamentos.LancarAsync(
            new NovoLancamentoModel { Valor = 250.00m, Tipo = "credito", Descricao = "venda balcão" },
            "mesma-chave", ct);

        Assert.Equal(repetida.Id, reenvio.Id);
        var lista = await lancamentos.ListarAsync(venda.Data, ct);
        Assert.Contains(lista, l => l.Id == venda.Id && l.PodeEstornar);
        // O comerciante de teste é compartilhado entre os testes da assembly (mesmo dia, mesmo
        // banco): conta-se só o que este teste criou, nunca o total do dia.
        Assert.Equal(2, lista.Count(l => l.Id == venda.Id || l.Id == repetida.Id));

        var estorno = await lancamentos.EstornarAsync(venda.Id, Guid.NewGuid().ToString(), ct);

        Assert.Equal("debito", estorno.Tipo);
        Assert.Equal(venda.Id, estorno.EstornoDe);
        var depois = await lancamentos.ListarAsync(venda.Data, ct);
        Assert.True(depois.Single(l => l.Id == venda.Id).Estornado);
        Assert.False(depois.Single(l => l.Id == venda.Id).PodeEstornar);
        Assert.False(depois.Single(l => l.Id == estorno.Id).PodeEstornar);
    }

    [Fact]
    [Trait("Criterio", "AC2")]
    public async Task A_api_recusa_o_perfil_consulta_mesmo_que_a_tela_chame()
    {
        await using var api = new WebApiFactory(db.ConnectionString);
        await using var front = new FrontendFactory(() => api.Server.CreateHandler());
        await using var sessao = front.Sessao(FrontendFactory.Consulta);
        var lancamentos = sessao.ServiceProvider.GetRequiredService<LancamentoService>();
        var ct = TestContext.Current.CancellationToken;

        var erro = await Assert.ThrowsAsync<ApiException>(() => lancamentos.LancarAsync(
            new NovoLancamentoModel { Valor = 10m, Tipo = "debito" }, Guid.NewGuid().ToString(), ct));

        Assert.Equal(HttpStatusCode.Forbidden, erro.StatusCode);
        Assert.NotNull(await lancamentos.ListarAsync(null, ct));
    }

    [Fact]
    [Trait("Criterio", "AC5")]
    public async Task Consolidado_do_dia_traz_saldo_do_dia_anterior_e_acumulado()
    {
        await using var api = new WebApiFactory(db.ConnectionString);
        await using var front = new FrontendFactory(() => api.Server.CreateHandler());
        await using var sessao = front.Sessao(FrontendFactory.Consulta);
        var consolidado = sessao.ServiceProvider.GetRequiredService<ConsolidadoService>();
        var hoje = sessao.ServiceProvider.GetRequiredService<DiaDeNegocio>().Hoje();

        var dia = await consolidado.ObterAsync(hoje, TestContext.Current.CancellationToken);

        Assert.Equal(hoje, dia.Data);
        Assert.Equal(FrontendFactory.Comerciante, dia.ComercianteId);
        Assert.Equal(dia.SaldoAnterior + dia.SaldoDia, dia.SaldoAcumulado);
    }

    [Fact]
    [Trait("Criterio", "AC3")]
    public async Task A_tela_de_lancamentos_renderiza_o_que_a_api_devolve()
    {
        await using var api = new WebApiFactory(db.ConnectionString);
        await using var front = new FrontendFactory(() => api.Server.CreateHandler());
        var ct = TestContext.Current.CancellationToken;
        await using (var sessao = front.Sessao(FrontendFactory.Operador))
        {
            await sessao.ServiceProvider.GetRequiredService<LancamentoService>().LancarAsync(
                new NovoLancamentoModel { Valor = 77.70m, Tipo = "debito", Descricao = "pagamento fornecedor xyz" },
                Guid.NewGuid().ToString(), ct);
        }

        using var navegador = front.Navegador();
        await FrontendFactory.EntrarAsync(navegador, "operador", FrontendFactory.Senha);
        var html = await navegador.GetStringAsync("/lancamentos", ct);

        Assert.Contains("pagamento fornecedor xyz", html);
        Assert.Contains("Estornar", html);
    }
}
