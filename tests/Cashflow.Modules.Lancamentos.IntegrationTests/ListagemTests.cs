using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Cashflow.Testing;

namespace Cashflow.Modules.Lancamentos.IntegrationTests;

/// <summary>PRD-0003 RF10: a lista do dia que a tela de estorno consome.</summary>
[Collection("integracao")]
public sealed class ListagemTests(PostgresFixture db)
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 11, 18, 2, 30, 0, TimeSpan.Zero); // São Paulo: 2026-11-17 23:30

    private static HttpClient Cliente(WebApiFactory factory, Guid comerciante, params string[] escopos)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestTokens.Create(comerciante, escopos));
        return client;
    }

    private static async Task<Guid> LancarAsync(HttpClient client, string tipo, decimal valor)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/lancamentos")
        {
            Content = JsonContent.Create(new { valor, tipo, descricao = "teste" })
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        return corpo.GetProperty("id").GetGuid();
    }

    private static async Task EstornarAsync(HttpClient client, Guid id)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/lancamentos/{id}/estornos");
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task RF10_lista_o_dia_do_comerciante_marcando_o_estornado()
    {
        var comerciante = Guid.NewGuid();
        var outro = Guid.NewGuid();
        using var factory = WebApiFactory.SoLancamentos(db.ConnectionString, FixedNow);
        using var escrita = Cliente(factory, comerciante, "lancamentos.write", "lancamentos.read");
        using var escritaOutro = Cliente(factory, outro, "lancamentos.write");

        var venda = await LancarAsync(escrita, "credito", 150.00m);
        await LancarAsync(escrita, "debito", 40.00m);
        await EstornarAsync(escrita, venda);
        await LancarAsync(escritaOutro, "credito", 999.00m);

        var lista = await escrita.GetFromJsonAsync<JsonElement[]>(
            "/api/v1/lancamentos?data=2026-11-17", TestContext.Current.CancellationToken);

        Assert.NotNull(lista);
        Assert.Equal(3, lista.Length);
        Assert.DoesNotContain(lista, l => l.GetProperty("valor").GetDecimal() == 999.00m);

        var original = Assert.Single(lista, l => l.GetProperty("id").GetGuid() == venda);
        Assert.True(original.GetProperty("estornado").GetBoolean());

        var estorno = Assert.Single(lista, l =>
            l.GetProperty("estornoDe").ValueKind == JsonValueKind.String
            && l.GetProperty("estornoDe").GetGuid() == venda);
        Assert.Equal("debito", estorno.GetProperty("tipo").GetString());
        Assert.False(estorno.GetProperty("estornado").GetBoolean());
    }

    [Fact]
    public async Task RF10_sem_data_usa_o_dia_de_negocio_de_sao_paulo()
    {
        var comerciante = Guid.NewGuid();
        using var factory = WebApiFactory.SoLancamentos(db.ConnectionString, FixedNow);
        using var client = Cliente(factory, comerciante, "lancamentos.write", "lancamentos.read");
        await LancarAsync(client, "credito", 10.00m);

        var lista = await client.GetFromJsonAsync<JsonElement[]>(
            "/api/v1/lancamentos", TestContext.Current.CancellationToken);

        var item = Assert.Single(lista!);
        Assert.Equal("2026-11-17", item.GetProperty("data").GetString());
    }

    [Fact]
    public async Task RF10_sem_escopo_de_leitura_responde_403()
    {
        using var factory = WebApiFactory.SoLancamentos(db.ConnectionString);
        using var client = Cliente(factory, Guid.NewGuid(), "lancamentos.write", "consolidado.read");

        var response = await client.GetAsync("/api/v1/lancamentos", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RF10_data_invalida_responde_400()
    {
        using var factory = WebApiFactory.SoLancamentos(db.ConnectionString);
        using var client = Cliente(factory, Guid.NewGuid(), "lancamentos.read");

        var response = await client.GetAsync("/api/v1/lancamentos?data=17-11-2026", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
