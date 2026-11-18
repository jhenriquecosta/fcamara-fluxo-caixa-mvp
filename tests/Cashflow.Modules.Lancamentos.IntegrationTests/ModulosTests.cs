using System.Net;
using Cashflow.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cashflow.Modules.Lancamentos.IntegrationTests;

/// <summary>O WebAPI implantado como lancamentos-api só expõe o módulo ligado (Modules:Consolidado:Enabled=false).</summary>
[Collection("integracao")]
public sealed class ModulosTests(PostgresFixture db)
{
    [Fact]
    public async Task Lancamentos_api_nao_expoe_o_consolidado()
    {
        using var factory = WebApiFactory.SoLancamentos(db.ConnectionString);
        using var client = factory.CreateClient().Autenticado(Guid.NewGuid(), "consolidado.read");

        var response = await client.GetAsync("/api/v1/consolidado/2026-11-17", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void WebAPI_nao_roda_trabalho_de_fundo()
    {
        using var factory = new WebApiFactory(db.ConnectionString);

        var trabalhos = factory.Services.GetServices<IHostedService>()
            .Select(s => s.GetType().FullName ?? "")
            .Where(n => n.StartsWith("Cashflow.", StringComparison.Ordinal))
            .ToList();

        Assert.True(trabalhos.Count == 0, $"o WebAPI subiu trabalho de fundo: {string.Join(", ", trabalhos)}");
    }
}
