using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cashflow.Testing;

namespace Cashflow.Modules.Consolidado.IntegrationTests;

/// <summary>PRD-0005 AC2: o deploy de Consolidado publica o contrato e só com as rotas dele.</summary>
[Collection("integracao")]
public sealed class OpenApiTests(PostgresFixture db)
{
    [Fact]
    [Trait("Criterio", "AC2")]
    public async Task O_documento_lista_as_rotas_de_consolidado_e_nao_as_de_lancamentos()
    {
        using var api = WebApiFactory.SoConsolidado(db.ConnectionString);
        using var anonimo = api.CreateClient();

        var resposta = await anonimo.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var json = await resposta.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        var caminhos = json.GetProperty("paths").EnumerateObject().Select(p => p.Name).ToList();
        Assert.Contains("/api/v1/consolidado/{data}", caminhos);
        Assert.DoesNotContain("/api/v1/lancamentos", caminhos);
        Assert.DoesNotContain("/api/v1/lancamentos/{id}/estornos", caminhos);
    }

    [Fact]
    [Trait("Criterio", "AC3")]
    public async Task A_consulta_do_consolidado_exige_o_escopo_de_leitura_do_consolidado()
    {
        using var api = WebApiFactory.SoConsolidado(db.ConnectionString);
        using var anonimo = api.CreateClient();

        var resposta = await anonimo.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var json = await resposta.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        var esquema = json.GetProperty("components").GetProperty("securitySchemes").GetProperty("OAuth2");
        Assert.True(esquema.GetProperty("flows").GetProperty("clientCredentials").GetProperty("scopes")
            .TryGetProperty("consolidado.read", out _));

        var consulta = json.GetProperty("paths").GetProperty("/api/v1/consolidado/{data}").GetProperty("get");
        Assert.Contains(consulta.GetProperty("security").EnumerateArray(),
            requisito => requisito.GetProperty("OAuth2").EnumerateArray()
                .Any(e => e.GetString() == "consolidado.read"));
    }
}
