using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cashflow.Testing;

namespace Cashflow.Modules.Lancamentos.IntegrationTests;

/// <summary>PRD-0005 AC1/AC3/AC4: o deploy de Lançamentos publica o contrato, só com as rotas dele.</summary>
[Collection("integracao")]
public sealed class OpenApiTests(PostgresFixture db)
{
    private static async Task<JsonElement> DocumentoAsync(HttpClient cliente)
    {
        var resposta = await cliente.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return await resposta.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
    }

    private static JsonElement Operacao(JsonElement documento, string caminho, string metodo) =>
        documento.GetProperty("paths").GetProperty(caminho).GetProperty(metodo);

    [Fact]
    [Trait("Criterio", "AC1")]
    public async Task O_documento_lista_as_rotas_de_lancamentos_e_nao_as_de_consolidado()
    {
        using var api = WebApiFactory.SoLancamentos(db.ConnectionString);
        using var anonimo = api.CreateClient();

        var json = await DocumentoAsync(anonimo);
        var caminhos = json.GetProperty("paths").EnumerateObject().Select(p => p.Name).ToList();
        Assert.Contains("/api/v1/lancamentos", caminhos);
        Assert.Contains("/api/v1/lancamentos/{id}/estornos", caminhos);
        Assert.DoesNotContain("/api/v1/consolidado/{data}", caminhos);
    }

    [Fact]
    [Trait("Criterio", "AC3")]
    public async Task O_documento_declara_oauth2_e_cada_operacao_exige_o_escopo_da_sua_politica()
    {
        using var api = WebApiFactory.SoLancamentos(db.ConnectionString);
        using var anonimo = api.CreateClient();

        var json = await DocumentoAsync(anonimo);
        var esquema = json.GetProperty("components").GetProperty("securitySchemes").GetProperty("OAuth2");
        Assert.Equal("oauth2", esquema.GetProperty("type").GetString());
        var escopos = esquema.GetProperty("flows").GetProperty("clientCredentials").GetProperty("scopes");
        Assert.True(escopos.TryGetProperty("lancamentos.write", out _));
        Assert.True(escopos.TryGetProperty("lancamentos.read", out _));
        Assert.True(escopos.TryGetProperty("consolidado.read", out _));

        Assert.Contains(Operacao(json, "/api/v1/lancamentos", "post").GetProperty("security").EnumerateArray(),
            requisito => Escopos(requisito).Contains("lancamentos.write"));
        Assert.Contains(Operacao(json, "/api/v1/lancamentos", "get").GetProperty("security").EnumerateArray(),
            requisito => Escopos(requisito).Contains("lancamentos.read"));

        var health = Operacao(json, "/health", "get");
        Assert.False(health.TryGetProperty("security", out var securityHealth) && securityHealth.GetArrayLength() > 0);
    }

    [Fact]
    [Trait("Criterio", "AC4")]
    public async Task Os_posts_declaram_a_chave_de_idempotencia_e_os_erros_saem_em_problem_details()
    {
        using var api = WebApiFactory.SoLancamentos(db.ConnectionString);
        using var anonimo = api.CreateClient();

        var json = await DocumentoAsync(anonimo);
        foreach (var caminho in new[] { "/api/v1/lancamentos", "/api/v1/lancamentos/{id}/estornos" })
        {
            var post = Operacao(json, caminho, "post");
            var chave = post.GetProperty("parameters").EnumerateArray()
                .Single(p => p.GetProperty("name").GetString() == "Idempotency-Key");
            Assert.Equal("header", chave.GetProperty("in").GetString());
            Assert.True(chave.GetProperty("required").GetBoolean());
            Assert.True(post.GetProperty("responses").TryGetProperty("400", out _));

            foreach (var codigo in new[] { "401", "403" })
            {
                Assert.True(post.GetProperty("responses").TryGetProperty(codigo, out var erro));
                Assert.True(erro.GetProperty("content").TryGetProperty("application/problem+json", out _));
            }
        }
    }

    private static List<string?> Escopos(JsonElement requisito) =>
        requisito.GetProperty("OAuth2").EnumerateArray().Select(e => e.GetString()).ToList();

    [Fact]
    [Trait("Criterio", "AC5")]
    public async Task A_ui_scalar_responde_em_development()
    {
        using var api = WebApiFactory.SoLancamentos(db.ConnectionString, ambiente: "Development");
        using var anonimo = api.CreateClient();

        var resposta = await anonimo.GetAsync("/scalar", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    [Trait("Criterio", "AC5")]
    public async Task A_ui_scalar_nao_e_mapeada_fora_de_development()
    {
        using var api = WebApiFactory.SoLancamentos(db.ConnectionString);
        using var anonimo = api.CreateClient();

        var resposta = await anonimo.GetAsync("/scalar", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }
}

