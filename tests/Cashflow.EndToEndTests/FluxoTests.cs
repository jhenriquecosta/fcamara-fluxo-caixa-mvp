using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cashflow.Testing;
using Confluent.Kafka;
using Dapper;
using Npgsql;

namespace Cashflow.EndToEndTests;

[Collection("integracao")]
public sealed class FluxoTests(PostgresFixture db, KafkaFixture kafka)
{
    private static readonly TimeSpan Prazo = TimeSpan.FromSeconds(40);

    [Fact]
    public async Task AC1_AC10_lancamento_chega_ao_consolidado()
    {
        var comerciante = Guid.NewGuid();
        using var topologia = new Topologia(db, kafka);
        using var clienteL = topologia.Lancamentos(comerciante);
        using var clienteC = topologia.Consolidado(comerciante);

        var resposta = await clienteL.PostAsJsonAsync("/api/v1/lancamentos",
            new { valor = 100.00m, tipo = "debito" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        var json = await clienteC.EsperarConsolidadoAsync(Topologia.Dia,
            j => j.GetProperty("saldoDia").GetDecimal() == -100.00m, Prazo);

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
        var ct = TestContext.Current.CancellationToken;
        using var topologia = new Topologia(db, kafka);
        using var clienteL = topologia.Lancamentos(comerciante);
        using var clienteC = topologia.Consolidado(comerciante);

        var resposta = await clienteL.PostAsJsonAsync("/api/v1/lancamentos",
            new { valor = 70.00m, tipo = "credito" }, ct);
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        Assert.NotNull(await clienteC.EsperarConsolidadoAsync(Topologia.Dia,
            j => j.GetProperty("saldoDia").GetDecimal() == 70.00m, Prazo));

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
        await produtor.ProduceAsync(KafkaFixture.Topico,
            new Message<string, string> { Key = comerciante.ToString(), Value = payload }, ct);
        produtor.Flush(TimeSpan.FromSeconds(5));

        await Task.Delay(2000, ct);
        var depois = await clienteC.GetFromJsonAsync<JsonElement>($"/api/v1/consolidado/{Topologia.Dia}", ct);
        Assert.Equal(70.00m, depois.GetProperty("saldoDia").GetDecimal());
    }

    [Fact]
    public async Task AC4_estorno_chega_ao_consolidado()
    {
        var comerciante = Guid.NewGuid();
        var ct = TestContext.Current.CancellationToken;
        using var topologia = new Topologia(db, kafka);
        using var clienteL = topologia.Lancamentos(comerciante);
        using var clienteC = topologia.Consolidado(comerciante);

        var originalResp = await clienteL.PostAsJsonAsync("/api/v1/lancamentos",
            new { valor = 50.00m, tipo = "credito" }, ct);
        var original = await originalResp.Content.ReadFromJsonAsync<JsonElement>(ct);
        var originalId = original.GetProperty("id").GetGuid();

        Assert.NotNull(await clienteC.EsperarConsolidadoAsync(Topologia.Dia,
            j => j.GetProperty("saldoDia").GetDecimal() == 50.00m, Prazo));

        using var clienteEstorno = topologia.Lancamentos(comerciante);
        var estornoResp = await clienteEstorno.PostAsync($"/api/v1/lancamentos/{originalId}/estornos", null, ct);
        Assert.Equal(HttpStatusCode.Created, estornoResp.StatusCode);

        var aposEstorno = await clienteC.EsperarConsolidadoAsync(Topologia.Dia,
            j => j.GetProperty("saldoDia").GetDecimal() == 0m, Prazo);
        Assert.NotNull(aposEstorno);
        Assert.Equal(0m, aposEstorno!.Value.GetProperty("saldoAcumulado").GetDecimal());
    }
}
