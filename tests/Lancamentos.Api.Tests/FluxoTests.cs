using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Confluent.Kafka;
using Dapper;
using Npgsql;

namespace Cashflow.Lancamentos.Api.Tests;

[Collection("integracao")]
public sealed class FluxoTests(PostgresFixture db, KafkaFixture kafka)
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 11, 18, 2, 30, 0, TimeSpan.Zero); // São Paulo: 2026-11-17 23:30

    private const string Dia = "2026-11-17";
    private const string Topico = "cashflow.lancamentos.v1";

    private static HttpClient Autenticado(HttpClient client, Guid comerciante, string escopo,
        string? chave = null)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", TestTokens.Create(comerciante, escopo));
        if (chave is not null)
            client.DefaultRequestHeaders.Add("Idempotency-Key", chave);
        return client;
    }

    private static async Task<JsonElement?> EsperarConsolidadoAsync(HttpClient cliente,
        Func<JsonElement, bool> condicao, TimeSpan timeout)
    {
        var limite = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < limite)
        {
            var resposta = await cliente.GetAsync($"/api/v1/consolidado/{Dia}",
                TestContext.Current.CancellationToken);
            if (resposta.StatusCode == HttpStatusCode.OK)
            {
                var json = await resposta.Content.ReadFromJsonAsync<JsonElement>(
                    TestContext.Current.CancellationToken);
                if (condicao(json))
                    return json;
            }
            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
        return null;
    }

    [Fact]
    public async Task AC1_AC10_lancamento_chega_ao_consolidado()
    {
        var comerciante = Guid.NewGuid();
        using var lancamentos = new ApiFactory(db.ConnectionString, FixedNow, kafka.BootstrapServers);
        using var consolidado = new ConsolidadoFactory(db.ConnectionString, kafka.BootstrapServers);
        using var clienteL = Autenticado(lancamentos.CreateClient(), comerciante,
            "lancamentos.write", Guid.NewGuid().ToString());
        using var clienteC = Autenticado(consolidado.CreateClient(), comerciante, "consolidado.read");

        var resposta = await clienteL.PostAsJsonAsync("/api/v1/lancamentos",
            new { valor = 100.00m, tipo = "debito" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        var json = await EsperarConsolidadoAsync(clienteC,
            j => j.GetProperty("saldoDia").GetDecimal() == -100.00m, TimeSpan.FromSeconds(40));

        Assert.NotNull(json);
        Assert.Equal(-100.00m, json!.Value.GetProperty("saldoDia").GetDecimal());
        Assert.Equal(0m, json.Value.GetProperty("saldoAnterior").GetDecimal());
        Assert.Equal(-100.00m, json.Value.GetProperty("saldoAcumulado").GetDecimal());
        Assert.Equal(comerciante, json.Value.GetProperty("comercianteId").GetGuid());
    }

    [Fact]
    public async Task AC6_evento_repetido_soma_uma_vez()
    {
        var comerciante = Guid.NewGuid();
        using var lancamentos = new ApiFactory(db.ConnectionString, FixedNow, kafka.BootstrapServers);
        using var consolidado = new ConsolidadoFactory(db.ConnectionString, kafka.BootstrapServers);
        using var clienteL = Autenticado(lancamentos.CreateClient(), comerciante,
            "lancamentos.write", Guid.NewGuid().ToString());
        using var clienteC = Autenticado(consolidado.CreateClient(), comerciante, "consolidado.read");

        var resposta = await clienteL.PostAsJsonAsync("/api/v1/lancamentos",
            new { valor = 70.00m, tipo = "credito" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        var json = await EsperarConsolidadoAsync(clienteC,
            j => j.GetProperty("saldoDia").GetDecimal() == 70.00m, TimeSpan.FromSeconds(40));
        Assert.NotNull(json);

        string payload;
        await using (var conn = new NpgsqlConnection(db.ConnectionString))
        {
            payload = await conn.ExecuteScalarAsync<string>("""
                SELECT payload::text FROM outbox_event
                WHERE payload->'payload'->>'comercianteId' = @m
                ORDER BY criado_em DESC LIMIT 1
                """, new { m = comerciante.ToString() }) ?? string.Empty;
        }
        Assert.False(string.IsNullOrWhiteSpace(payload));

        using var produtor = new ProducerBuilder<string, string>(
            new ProducerConfig { BootstrapServers = kafka.BootstrapServers }).Build();
        await produtor.ProduceAsync(Topico,
            new Message<string, string> { Key = comerciante.ToString(), Value = payload },
            TestContext.Current.CancellationToken);
        produtor.Flush(TimeSpan.FromSeconds(5));

        await Task.Delay(2000, TestContext.Current.CancellationToken);
        var depois = await clienteC.GetFromJsonAsync<JsonElement>(
            $"/api/v1/consolidado/{Dia}", TestContext.Current.CancellationToken);
        Assert.Equal(70.00m, depois.GetProperty("saldoDia").GetDecimal());
    }
}
