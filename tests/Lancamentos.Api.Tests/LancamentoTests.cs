using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Cashflow.Lancamentos.Application;
using Cashflow.Lancamentos.Domain;
using Dapper;
using Npgsql;

namespace Cashflow.Lancamentos.Api.Tests;

[Collection("integracao")]
public sealed class LancamentoTests(PostgresFixture db)
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 11, 18, 2, 30, 0, TimeSpan.Zero); // São Paulo: 2026-11-17 23:30

    private static HttpClient ClientComEscopo(ApiFactory factory, Guid comerciante, string chave)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", TestTokens.Create(comerciante, "lancamentos.write"));
        client.DefaultRequestHeaders.Add("Idempotency-Key", chave);
        return client;
    }

    private async Task<int> ContarAsync(string sql, object param)
    {
        await using var conn = new NpgsqlConnection(db.ConnectionString);
        return await conn.ExecuteScalarAsync<int>(sql, param);
    }

    [Fact]
    public async Task AC1_lancamento_valido_responde_201_com_o_dia_do_servidor()
    {
        var comerciante = Guid.NewGuid();
        using var factory = new ApiFactory(db.ConnectionString, FixedNow);
        using var client = ClientComEscopo(factory, comerciante, Guid.NewGuid().ToString());

        var response = await client.PostAsJsonAsync("/api/v1/lancamentos",
            new { valor = 100.00m, tipo = "debito", descricao = "venda" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);
        Assert.Equal("2026-11-17", corpo.GetProperty("data").GetString());
        Assert.Equal(100.00m, corpo.GetProperty("valor").GetDecimal());
        Assert.Equal("debito", corpo.GetProperty("tipo").GetString());
        Assert.NotEqual(Guid.Empty, corpo.GetProperty("id").GetGuid());
        Assert.Equal(comerciante, corpo.GetProperty("comercianteId").GetGuid());

        var lancamentos = await ContarAsync(
            "SELECT count(*) FROM lancamento WHERE comerciante_id = @m",
            new { m = comerciante });
        var eventos = await ContarAsync(
            "SELECT count(*) FROM outbox_event WHERE payload->'payload'->>'comercianteId' = @m",
            new { m = comerciante.ToString() });

        Assert.Equal(1, lancamentos);
        Assert.Equal(1, eventos);
    }

    [Fact]
    public async Task RF_valor_ou_tipo_invalido_responde_422()
    {
        using var factory = new ApiFactory(db.ConnectionString, FixedNow);
        using var client = ClientComEscopo(factory, Guid.NewGuid(), Guid.NewGuid().ToString());

        var valorZero = await client.PostAsJsonAsync("/api/v1/lancamentos",
            new { valor = 0.00m, tipo = "debito" }, TestContext.Current.CancellationToken);
        var tipoInvalido = await client.PostAsJsonAsync("/api/v1/lancamentos",
            new { valor = 10.00m, tipo = "invalido" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, valorZero.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, tipoInvalido.StatusCode);
    }

    [Fact]
    public async Task AC2_mesma_chave_e_corpo_devolve_a_resposta_original_sem_duplicar()
    {
        var comerciante = Guid.NewGuid();
        var chave = Guid.NewGuid().ToString();
        using var factory = new ApiFactory(db.ConnectionString, FixedNow);
        using var client = ClientComEscopo(factory, comerciante, chave);

        var body = new { valor = 50.00m, tipo = "credito", descricao = "recebimento" };
        var primeiro = await client.PostAsJsonAsync("/api/v1/lancamentos", body,
            TestContext.Current.CancellationToken);
        var segundo = await client.PostAsJsonAsync("/api/v1/lancamentos", body,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, primeiro.StatusCode);
        Assert.Equal(HttpStatusCode.Created, segundo.StatusCode);

        var id1 = (await primeiro.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken))
            .GetProperty("id").GetGuid();
        var id2 = (await segundo.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken))
            .GetProperty("id").GetGuid();
        Assert.Equal(id1, id2);

        var total = await ContarAsync(
            "SELECT count(*) FROM lancamento WHERE comerciante_id = @m", new { m = comerciante });
        Assert.Equal(1, total);
    }

    [Fact]
    public async Task AC3_mesma_chave_com_corpo_diferente_responde_422()
    {
        var comerciante = Guid.NewGuid();
        var chave = Guid.NewGuid().ToString();
        using var factory = new ApiFactory(db.ConnectionString, FixedNow);
        using var client = ClientComEscopo(factory, comerciante, chave);

        var primeiro = await client.PostAsJsonAsync("/api/v1/lancamentos",
            new { valor = 100.00m, tipo = "debito" }, TestContext.Current.CancellationToken);
        var segundo = await client.PostAsJsonAsync("/api/v1/lancamentos",
            new { valor = 200.00m, tipo = "debito" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, primeiro.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, segundo.StatusCode);
    }

    [Fact]
    public async Task AC13_chave_em_processamento_responde_409()
    {
        var comerciante = Guid.NewGuid();
        var chave = Guid.NewGuid().ToString();
        var hash = LancamentoCorpo.Hash(TipoLancamento.Debito, 100.00m, null);

        await using (var conn = new NpgsqlConnection(db.ConnectionString))
        {
            await conn.ExecuteAsync("""
                INSERT INTO idempotencia (comerciante_id, chave, corpo_hash, status, processando_desde)
                VALUES (@m, @k, @h, 'processando', now())
                """, new { m = comerciante, k = chave, h = hash });
        }

        using var factory = new ApiFactory(db.ConnectionString, FixedNow);
        using var client = ClientComEscopo(factory, comerciante, chave);
        var response = await client.PostAsJsonAsync("/api/v1/lancamentos",
            new { valor = 100.00m, tipo = "debito" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AC4_estorno_cria_contra_lancamento_e_preserva_o_original()
    {
        var comerciante = Guid.NewGuid();
        using var factory = new ApiFactory(db.ConnectionString, FixedNow);
        using var client = ClientComEscopo(factory, comerciante, Guid.NewGuid().ToString());

        var originalResp = await client.PostAsJsonAsync("/api/v1/lancamentos",
            new { valor = 100.00m, tipo = "credito" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, originalResp.StatusCode);
        var original = await originalResp.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);
        var originalId = original.GetProperty("id").GetGuid();

        using var estornoClient = ClientComEscopo(factory, comerciante, Guid.NewGuid().ToString());
        var estornoResp = await estornoClient.PostAsync(
            $"/api/v1/lancamentos/{originalId}/estornos", null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, estornoResp.StatusCode);
        var estorno = await estornoResp.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);
        Assert.Equal("debito", estorno.GetProperty("tipo").GetString());
        Assert.Equal(100.00m, estorno.GetProperty("valor").GetDecimal());
        Assert.Equal(originalId, estorno.GetProperty("estornoDe").GetGuid());

        await using var conn = new NpgsqlConnection(db.ConnectionString);
        var originalDb = await conn.QuerySingleAsync<LancamentoRow>(
            "SELECT tipo AS Tipo, valor AS Valor, estorno_de AS EstornoDe FROM lancamento WHERE id = @id",
            new { id = originalId });
        var estornoDb = await conn.QuerySingleAsync<LancamentoRow>(
            "SELECT tipo AS Tipo, valor AS Valor, estorno_de AS EstornoDe FROM lancamento WHERE estorno_de = @id",
            new { id = originalId });

        Assert.Equal("credito", originalDb.Tipo);
        Assert.Equal(100.00m, originalDb.Valor);
        Assert.Null(originalDb.EstornoDe);
        Assert.Equal("debito", estornoDb.Tipo);
        Assert.Equal(originalId, estornoDb.EstornoDe);
    }

    [Fact]
    public async Task AC5_segundo_estorno_409_e_estorno_de_estorno_422()
    {
        var comerciante = Guid.NewGuid();
        using var factory = new ApiFactory(db.ConnectionString, FixedNow);
        using var client = ClientComEscopo(factory, comerciante, Guid.NewGuid().ToString());

        var originalResp = await client.PostAsJsonAsync("/api/v1/lancamentos",
            new { valor = 40.00m, tipo = "debito" }, TestContext.Current.CancellationToken);
        var original = await originalResp.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);
        var originalId = original.GetProperty("id").GetGuid();

        using var estornoClient = ClientComEscopo(factory, comerciante, Guid.NewGuid().ToString());
        var primeiro = await estornoClient.PostAsync(
            $"/api/v1/lancamentos/{originalId}/estornos", null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, primeiro.StatusCode);
        var primeiroJson = await primeiro.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);
        var estornoId = primeiroJson.GetProperty("id").GetGuid();

        using var segundoClient = ClientComEscopo(factory, comerciante, Guid.NewGuid().ToString());
        var segundo = await segundoClient.PostAsync(
            $"/api/v1/lancamentos/{originalId}/estornos", null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, segundo.StatusCode);

        using var terceiroClient = ClientComEscopo(factory, comerciante, Guid.NewGuid().ToString());
        var estornoDeEstorno = await terceiroClient.PostAsync(
            $"/api/v1/lancamentos/{estornoId}/estornos", null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, estornoDeEstorno.StatusCode);
    }

    [Fact]
    public async Task Metricas_expoem_o_contador_de_lancamentos()
    {
        using var factory = new ApiFactory(db.ConnectionString, FixedNow);
        using var client = ClientComEscopo(factory, Guid.NewGuid(), Guid.NewGuid().ToString());
        await client.PostAsJsonAsync("/api/v1/lancamentos",
            new { valor = 10.00m, tipo = "debito" }, TestContext.Current.CancellationToken);

        using var anonimo = factory.CreateClient();
        var metrics = await anonimo.GetAsync("/metrics", TestContext.Current.CancellationToken);
        var texto = await metrics.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, metrics.StatusCode);
        Assert.Contains("cashflow_lancamentos_total", texto);
    }
}

internal sealed record LancamentoRow(string Tipo, decimal Valor, Guid? EstornoDe);
