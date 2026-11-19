using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cashflow.Testing;
using Confluent.Kafka;
using Dapper;
using Npgsql;

namespace Cashflow.EndToEndTests;

/// <summary>
/// O caminho inteiro, com Kafka e PostgreSQL reais: lançamento na API, outbox, publicador, tópico,
/// consumidor e consulta do consolidado. A projeção é eventual, então a consulta espera até o saldo aparecer.
/// </summary>
[Collection("integracao")]
public sealed class FluxoTests(PostgresFixture db, KafkaFixture kafka)
{
    private static readonly TimeSpan PrazoDaProjecao = TimeSpan.FromSeconds(40);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    [Trait("Criterio", "AC1")]
    [Trait("Criterio", "AC10")]
    public async Task Debito_registrado_aparece_no_consolidado_do_dia()
    {
        // Arrange
        var comerciante = Guid.NewGuid();
        using var topologia = new Topologia(db, kafka);
        using var lancamentos = topologia.Lancamentos(comerciante);
        using var consolidado = topologia.Consolidado(comerciante);

        // Act
        var resposta = await lancamentos.PostAsJsonAsync("/api/v1/lancamentos", new { valor = 100.00m, tipo = "debito" }, Ct);

        // Assert
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var dia = await consolidado.EsperarConsolidadoAsync(Topologia.Dia,
            j => j.GetProperty("saldoDia").GetDecimal() == -100.00m, PrazoDaProjecao);
        Assert.NotNull(dia);
        Assert.Equal(comerciante, dia!.Value.GetProperty("comercianteId").GetGuid());
        Assert.Equal(0m, dia.Value.GetProperty("saldoAnterior").GetDecimal());
        Assert.Equal(-100.00m, dia.Value.GetProperty("saldoAcumulado").GetDecimal());
    }

    [Fact]
    [Trait("Criterio", "AC6")]
    public async Task Evento_entregue_duas_vezes_pelo_Kafka_soma_uma_vez_so()
    {
        // Arrange: um crédito de 70 já projetado
        var comerciante = Guid.NewGuid();
        using var topologia = new Topologia(db, kafka);
        using var lancamentos = topologia.Lancamentos(comerciante);
        using var consolidado = topologia.Consolidado(comerciante);
        await lancamentos.PostAsJsonAsync("/api/v1/lancamentos", new { valor = 70.00m, tipo = "credito" }, Ct);
        Assert.NotNull(await consolidado.EsperarConsolidadoAsync(Topologia.Dia,
            j => j.GetProperty("saldoDia").GetDecimal() == 70.00m, PrazoDaProjecao));

        // Act: o mesmo envelope, com o mesmo eventId, publicado de novo no tópico
        await RepublicarUltimoEventoAsync(comerciante);
        await Task.Delay(TimeSpan.FromSeconds(2), Ct);

        // Assert
        var depois = await consolidado.GetFromJsonAsync<JsonElement>($"/api/v1/consolidado/{Topologia.Dia}", Ct);
        Assert.Equal(70.00m, depois.GetProperty("saldoDia").GetDecimal());
    }

    [Fact]
    [Trait("Criterio", "AC4")]
    public async Task Estorno_anula_o_lancamento_no_consolidado()
    {
        // Arrange: um crédito de 50 já projetado
        var comerciante = Guid.NewGuid();
        using var topologia = new Topologia(db, kafka);
        using var lancamentos = topologia.Lancamentos(comerciante);
        using var consolidado = topologia.Consolidado(comerciante);
        var original = await (await lancamentos.PostAsJsonAsync("/api/v1/lancamentos",
            new { valor = 50.00m, tipo = "credito" }, Ct)).Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.NotNull(await consolidado.EsperarConsolidadoAsync(Topologia.Dia,
            j => j.GetProperty("saldoDia").GetDecimal() == 50.00m, PrazoDaProjecao));

        // Act
        using var estornando = topologia.Lancamentos(comerciante);
        var estorno = await estornando.PostAsync(
            $"/api/v1/lancamentos/{original.GetProperty("id").GetGuid()}/estornos", null, Ct);

        // Assert
        Assert.Equal(HttpStatusCode.Created, estorno.StatusCode);
        var dia = await consolidado.EsperarConsolidadoAsync(Topologia.Dia,
            j => j.GetProperty("saldoDia").GetDecimal() == 0m, PrazoDaProjecao);
        Assert.NotNull(dia);
        Assert.Equal(0m, dia!.Value.GetProperty("saldoAcumulado").GetDecimal());
    }

    /// <summary>Lê do outbox o último envelope do comerciante e o produz de novo no tópico, como numa reentrega.</summary>
    private async Task RepublicarUltimoEventoAsync(Guid comerciante)
    {
        string envelope;
        await using (var conn = new NpgsqlConnection(db.ConnectionString))
        {
            envelope = await conn.ExecuteScalarAsync<string>("""
                SELECT payload::text FROM outbox_event
                WHERE payload->'payload'->>'comercianteId' = @comerciante
                ORDER BY criado_em DESC LIMIT 1
                """, new { comerciante = comerciante.ToString() }) ?? string.Empty;
        }
        Assert.False(string.IsNullOrWhiteSpace(envelope), "o evento não está no outbox");

        using var produtor = new ProducerBuilder<string, string>(
            new ProducerConfig { BootstrapServers = kafka.BootstrapServers }).Build();
        await produtor.ProduceAsync(KafkaFixture.Topico,
            new Message<string, string> { Key = comerciante.ToString(), Value = envelope }, Ct);
        produtor.Flush(TimeSpan.FromSeconds(5));
    }
}
