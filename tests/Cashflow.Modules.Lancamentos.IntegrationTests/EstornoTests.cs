using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cashflow.Testing;
using Dapper;
using Npgsql;

namespace Cashflow.Modules.Lancamentos.IntegrationTests;

/// <summary>
/// O estorno pela API: corrige com um contra-lançamento, preserva o original e só vale uma vez por
/// lançamento.
/// </summary>
[Collection("integracao")]
public sealed class EstornoTests(PostgresFixture db)
{
    private static readonly DateTimeOffset AgoraNoServidor = new(2026, 11, 18, 2, 30, 0, TimeSpan.Zero);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private sealed record LinhaDoLancamento(string Tipo, decimal Valor, Guid? EstornoDe);

    /// <summary>Cada chamada usa uma chave nova, como um cliente que faz pedidos diferentes.</summary>
    private static HttpClient NovoCliente(WebApiFactory api, Guid comerciante) =>
        api.CreateClient().Autenticado(comerciante, "lancamentos.write", Guid.NewGuid().ToString());

    private static async Task<Guid> RegistrarAsync(WebApiFactory api, Guid comerciante, string tipo, decimal valor)
    {
        using var cliente = NovoCliente(api, comerciante);
        var resposta = await cliente.PostAsJsonAsync("/api/v1/lancamentos", new { valor, tipo }, Ct);
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("id").GetGuid();
    }

    private static async Task<HttpResponseMessage> EstornarAsync(WebApiFactory api, Guid comerciante, Guid lancamentoId)
    {
        using var cliente = NovoCliente(api, comerciante);
        return await cliente.PostAsync($"/api/v1/lancamentos/{lancamentoId}/estornos", null, Ct);
    }

    [Fact]
    [Trait("Criterio", "AC4")]
    public async Task Estorno_responde_201_com_um_contra_lancamento_de_tipo_oposto()
    {
        // Arrange
        var comerciante = Guid.NewGuid();
        using var api = WebApiFactory.SoLancamentos(db.ConnectionString, AgoraNoServidor);
        var original = await RegistrarAsync(api, comerciante, "credito", 100.00m);

        // Act
        var resposta = await EstornarAsync(api, comerciante, original);

        // Assert
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var estorno = await resposta.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal("debito", estorno.GetProperty("tipo").GetString());
        Assert.Equal(100.00m, estorno.GetProperty("valor").GetDecimal());
        Assert.Equal(original, estorno.GetProperty("estornoDe").GetGuid());
    }

    [Fact]
    [Trait("Criterio", "AC4")]
    public async Task Estorno_deixa_o_original_intacto_no_banco()
    {
        // Arrange
        var comerciante = Guid.NewGuid();
        using var api = WebApiFactory.SoLancamentos(db.ConnectionString, AgoraNoServidor);
        var original = await RegistrarAsync(api, comerciante, "credito", 100.00m);

        // Act
        await EstornarAsync(api, comerciante, original);

        // Assert
        await using var conn = new NpgsqlConnection(db.ConnectionString);
        var linhaOriginal = await conn.QuerySingleAsync<LinhaDoLancamento>(
            "SELECT tipo AS Tipo, valor AS Valor, estorno_de AS EstornoDe FROM lancamento WHERE id = @original",
            new { original });
        Assert.Equal(new LinhaDoLancamento("credito", 100.00m, null), linhaOriginal);
    }

    [Fact]
    [Trait("Criterio", "AC5")]
    public async Task Segundo_estorno_do_mesmo_lancamento_responde_409()
    {
        // Arrange
        var comerciante = Guid.NewGuid();
        using var api = WebApiFactory.SoLancamentos(db.ConnectionString, AgoraNoServidor);
        var original = await RegistrarAsync(api, comerciante, "debito", 40.00m);
        Assert.Equal(HttpStatusCode.Created, (await EstornarAsync(api, comerciante, original)).StatusCode);

        // Act
        var segundo = await EstornarAsync(api, comerciante, original);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, segundo.StatusCode);
    }

    [Fact]
    [Trait("Criterio", "AC5")]
    public async Task Estornar_um_estorno_responde_422()
    {
        // Arrange
        var comerciante = Guid.NewGuid();
        using var api = WebApiFactory.SoLancamentos(db.ConnectionString, AgoraNoServidor);
        var original = await RegistrarAsync(api, comerciante, "debito", 40.00m);
        var estorno = (await (await EstornarAsync(api, comerciante, original))
            .Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("id").GetGuid();

        // Act
        var estornoDoEstorno = await EstornarAsync(api, comerciante, estorno);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, estornoDoEstorno.StatusCode);
    }

    [Fact]
    public async Task Estornar_lancamento_de_outro_comerciante_responde_404_como_se_nao_existisse()
    {
        // Arrange
        using var api = WebApiFactory.SoLancamentos(db.ConnectionString, AgoraNoServidor);
        var lancamentoDeOutro = await RegistrarAsync(api, Guid.NewGuid(), "credito", 10.00m);

        // Act
        var resposta = await EstornarAsync(api, Guid.NewGuid(), lancamentoDeOutro);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }
}
