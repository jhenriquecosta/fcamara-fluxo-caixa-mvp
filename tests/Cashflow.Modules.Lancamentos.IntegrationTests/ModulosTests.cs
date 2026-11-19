using System.Net;
using Cashflow.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cashflow.Modules.Lancamentos.IntegrationTests;

/// <summary>
/// O WebAPI é um binário só, implantado com módulos diferentes ligados. Implantado como
/// <c>lancamentos-api</c>, ele não pode expor o consolidado nem rodar trabalho de fundo.
/// </summary>
[Collection("integracao")]
public sealed class ModulosTests(PostgresFixture db)
{
    [Fact]
    [Trait("Criterio", "AC8")]
    public async Task Lancamentos_api_nao_expoe_a_rota_do_consolidado()
    {
        using var api = WebApiFactory.SoLancamentos(db.ConnectionString);
        using var cliente = api.CreateClient().Autenticado(Guid.NewGuid(), "consolidado.read");

        var resposta = await cliente.GetAsync("/api/v1/consolidado/2026-11-17", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public void WebAPI_nao_sobe_trabalho_de_fundo_porque_o_outbox_e_o_consumidor_sao_do_BackgroundServer()
    {
        using var api = new WebApiFactory(db.ConnectionString);

        var trabalhosDeFundo = api.Services.GetServices<IHostedService>()
            .Select(s => s.GetType().FullName ?? "")
            .Where(n => n.StartsWith("Cashflow.", StringComparison.Ordinal))
            .ToList();

        Assert.True(trabalhosDeFundo.Count == 0, $"o WebAPI subiu trabalho de fundo: {string.Join(", ", trabalhosDeFundo)}");
    }
}
