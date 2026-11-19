using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cashflow.Modules.Lancamentos.Commands;
using Cashflow.Modules.Lancamentos.Entities;
using Cashflow.Testing;
using Dapper;
using Npgsql;

namespace Cashflow.Modules.Lancamentos.IntegrationTests;

/// <summary>
/// O registro de lançamentos pela API, contra um PostgreSQL real: o que a resposta diz e o que fica
/// gravado no banco (lançamento, outbox e chave de idempotência).
/// </summary>
[Collection("integracao")]
public sealed class LancamentoTests(PostgresFixture db)
{
    private const string Rota = "/api/v1/lancamentos";

    /// <summary>02h30 de 18/11 em UTC, que é 23h30 de 17/11 em São Paulo.</summary>
    private static readonly DateTimeOffset AgoraNoServidor = new(2026, 11, 18, 2, 30, 0, TimeSpan.Zero);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private WebApiFactory ApiDeLancamentos() => WebApiFactory.SoLancamentos(db.ConnectionString, AgoraNoServidor);

    private static HttpClient Cliente(WebApiFactory api, Guid comerciante, string? chave = null) =>
        api.CreateClient().Autenticado(comerciante, "lancamentos.write", chave ?? Guid.NewGuid().ToString());

    private async Task<int> ContarAsync(string sql, object parametros)
    {
        await using var conn = new NpgsqlConnection(db.ConnectionString);
        return await conn.ExecuteScalarAsync<int>(sql, parametros);
    }

    [Fact]
    [Trait("Criterio", "AC1")]
    public async Task Lancamento_valido_responde_201_no_dia_de_negocio_de_Sao_Paulo()
    {
        // Arrange
        var comerciante = Guid.NewGuid();
        using var api = ApiDeLancamentos();
        using var cliente = Cliente(api, comerciante);

        // Act
        var resposta = await cliente.PostAsJsonAsync(Rota, new { valor = 100.00m, tipo = "debito", descricao = "venda" }, Ct);

        // Assert
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var lancamento = await resposta.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.NotEqual(Guid.Empty, lancamento.GetProperty("id").GetGuid());
        Assert.Equal(comerciante, lancamento.GetProperty("comercianteId").GetGuid());
        Assert.Equal("2026-11-17", lancamento.GetProperty("data").GetString());
        Assert.Equal(100.00m, lancamento.GetProperty("valor").GetDecimal());
        Assert.Equal("debito", lancamento.GetProperty("tipo").GetString());
    }

    [Fact]
    [Trait("Criterio", "AC1")]
    public async Task Lancamento_e_evento_do_outbox_sao_gravados_juntos()
    {
        // Arrange
        var comerciante = Guid.NewGuid();
        using var api = ApiDeLancamentos();
        using var cliente = Cliente(api, comerciante);

        // Act
        await cliente.PostAsJsonAsync(Rota, new { valor = 100.00m, tipo = "debito" }, Ct);

        // Assert
        Assert.Equal(1, await ContarAsync(
            "SELECT count(*) FROM lancamento WHERE comerciante_id = @comerciante", new { comerciante }));
        Assert.Equal(1, await ContarAsync(
            "SELECT count(*) FROM outbox_event WHERE payload->'payload'->>'comercianteId' = @comerciante",
            new { comerciante = comerciante.ToString() }));
    }

    [Theory]
    [InlineData(0.00, "debito")]
    [InlineData(-10.00, "credito")]
    [InlineData(10.00, "invalido")]
    [InlineData(10.00, "1")]
    public async Task Valor_nao_positivo_ou_tipo_fora_do_contrato_responde_422(decimal valor, string tipo)
    {
        using var api = ApiDeLancamentos();
        using var cliente = Cliente(api, Guid.NewGuid());

        var resposta = await cliente.PostAsJsonAsync(Rota, new { valor, tipo }, Ct);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
    }

    [Fact]
    public async Task Escrita_sem_Idempotency_Key_responde_400()
    {
        using var api = ApiDeLancamentos();
        using var cliente = api.CreateClient().Autenticado(Guid.NewGuid(), "lancamentos.write");

        var resposta = await cliente.PostAsJsonAsync(Rota, new { valor = 10.00m, tipo = "debito" }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    [Trait("Criterio", "AC2")]
    public async Task Repetir_a_requisicao_com_a_mesma_chave_devolve_o_mesmo_lancamento_sem_gravar_outro()
    {
        // Arrange
        var comerciante = Guid.NewGuid();
        using var api = ApiDeLancamentos();
        using var cliente = Cliente(api, comerciante, chave: Guid.NewGuid().ToString());
        var corpo = new { valor = 50.00m, tipo = "credito", descricao = "recebimento" };

        // Act
        var primeira = await cliente.PostAsJsonAsync(Rota, corpo, Ct);
        var repetida = await cliente.PostAsJsonAsync(Rota, corpo, Ct);

        // Assert
        Assert.Equal(HttpStatusCode.Created, primeira.StatusCode);
        Assert.Equal(HttpStatusCode.Created, repetida.StatusCode);
        Assert.Equal(
            (await primeira.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("id").GetGuid(),
            (await repetida.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("id").GetGuid());
        Assert.Equal(1, await ContarAsync(
            "SELECT count(*) FROM lancamento WHERE comerciante_id = @comerciante", new { comerciante }));
    }

    [Fact]
    [Trait("Criterio", "AC3")]
    public async Task Reusar_a_chave_com_outro_corpo_responde_422()
    {
        // Arrange
        using var api = ApiDeLancamentos();
        using var cliente = Cliente(api, Guid.NewGuid(), chave: Guid.NewGuid().ToString());
        await cliente.PostAsJsonAsync(Rota, new { valor = 100.00m, tipo = "debito" }, Ct);

        // Act
        var comOutroCorpo = await cliente.PostAsJsonAsync(Rota, new { valor = 200.00m, tipo = "debito" }, Ct);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, comOutroCorpo.StatusCode);
    }

    [Fact]
    [Trait("Criterio", "AC13")]
    public async Task Chave_ainda_em_processamento_por_outra_requisicao_responde_409()
    {
        // Arrange: outra requisição reservou a chave agora há pouco e não terminou
        var comerciante = Guid.NewGuid();
        var chave = Guid.NewGuid().ToString();
        await using (var conn = new NpgsqlConnection(db.ConnectionString))
        {
            await conn.ExecuteAsync("""
                INSERT INTO idempotencia (comerciante_id, chave, corpo_hash, status, processando_desde)
                VALUES (@comerciante, @chave, @impressao, 'processando', now())
                """, new
            {
                comerciante,
                chave,
                impressao = RegistrarLancamentoCommand.ImpressaoDigitalDoCorpo(TipoLancamento.Debito, 100.00m, null)
            });
        }

        using var api = ApiDeLancamentos();
        using var cliente = Cliente(api, comerciante, chave);

        // Act
        var resposta = await cliente.PostAsJsonAsync(Rota, new { valor = 100.00m, tipo = "debito" }, Ct);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task O_endpoint_de_metricas_expoe_o_contador_de_lancamentos()
    {
        // Arrange
        using var api = ApiDeLancamentos();
        using var cliente = Cliente(api, Guid.NewGuid());
        await cliente.PostAsJsonAsync(Rota, new { valor = 10.00m, tipo = "debito" }, Ct);

        // Act
        using var prometheus = api.CreateClient();
        var metricas = await prometheus.GetAsync("/metrics", Ct);

        // Assert
        Assert.Equal(HttpStatusCode.OK, metricas.StatusCode);
        Assert.Contains("cashflow_lancamentos_total", await metricas.Content.ReadAsStringAsync(Ct));
    }
}
