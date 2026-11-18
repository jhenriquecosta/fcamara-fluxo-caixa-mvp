using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Confluent.Kafka;
using Dapper;
using Npgsql;

namespace Cashflow.Lancamentos.Api.Tests;

[Collection("integracao")]
public sealed class ResilienciaTests(PostgresFixture db, KafkaFixture kafka)
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 11, 18, 2, 30, 0, TimeSpan.Zero);

    private const string Dia = "2026-11-17";
    private const string Topico = "cashflow.lancamentos.v1";
    private const string TopicoDlt = "cashflow.lancamentos.v1.dlt";

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
    public async Task AC7_evento_com_falha_persistente_vai_para_a_dlt()
    {
        using var consolidado = new ConsolidadoFactory(db.ConnectionString, kafka.BootstrapServers);

        using var produtor = new ProducerBuilder<string, string>(
            new ProducerConfig { BootstrapServers = kafka.BootstrapServers }).Build();
        await produtor.ProduceAsync(Topico, new Message<string, string>
        {
            Key = "malformado",
            Value = "{ nao-e-json"
        }, TestContext.Current.CancellationToken);
        produtor.Flush(TimeSpan.FromSeconds(5));

        using var clienteMetricas = consolidado.CreateClient();
        var limite = DateTime.UtcNow.AddSeconds(30);
        var dltConfirmado = false;
        while (DateTime.UtcNow < limite && !dltConfirmado)
        {
            var resposta = await clienteMetricas.GetAsync("/metrics",
                TestContext.Current.CancellationToken);
            var texto = await resposta.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken);

            var linha = texto.Split('\n')
                .FirstOrDefault(l => l.StartsWith("cashflow_dlt_events_total ")
                                     || l.StartsWith("cashflow_dlt_events_total{"));
            if (linha is not null)
            {
                var valor = linha.Split(' ', StringSplitOptions.RemoveEmptyEntries).Last();
                dltConfirmado = double.TryParse(valor, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var v) && v > 0;
            }

            if (!dltConfirmado)
                await Task.Delay(500, TestContext.Current.CancellationToken);
        }

        Assert.True(dltConfirmado, "o evento não chegou à DLT");

    }

    [Fact]
    public async Task AC8_lancamentos_segue_de_pe_com_o_consolidado_fora()
    {
        var comerciante = Guid.NewGuid();
        using var lancamentos = new ApiFactory(db.ConnectionString, FixedNow, kafka.BootstrapServers);
        using var cliente = Autenticado(lancamentos.CreateClient(), comerciante,
            "lancamentos.write", Guid.NewGuid().ToString());

        var resposta = await cliente.PostAsJsonAsync("/api/v1/lancamentos",
            new { valor = 30.00m, tipo = "credito" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        await Task.Delay(2000, TestContext.Current.CancellationToken);
        await using (var conn = new NpgsqlConnection(db.ConnectionString))
        {
            var publicado = await conn.ExecuteScalarAsync<DateTime?>("""
                SELECT publicado_em FROM outbox_event
                WHERE payload->'payload'->>'comercianteId' = @m
                ORDER BY criado_em DESC LIMIT 1
                """, new { m = comerciante.ToString() });
            Assert.NotNull(publicado);
        }

        using var consolidado = new ConsolidadoFactory(db.ConnectionString, kafka.BootstrapServers);
        using var clienteConsolidado = Autenticado(consolidado.CreateClient(), comerciante, "consolidado.read");
        var json = await EsperarConsolidadoAsync(clienteConsolidado,
            j => j.GetProperty("saldoDia").GetDecimal() == 30.00m, TimeSpan.FromSeconds(40));
        Assert.NotNull(json);
    }

    [Fact]
    public async Task AC9_consolidado_sustenta_50_requisicoes_por_segundo()
    {
        using var factory = new ConsolidadoFactory(db.ConnectionString, "");
        using var cliente = Autenticado(factory.CreateClient(), Guid.NewGuid(), "consolidado.read");

        var total = 0;
        var sucesso = 0;
        var inicio = DateTime.UtcNow;
        var fim = inicio.AddSeconds(10);

        using var relogio = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
        while (DateTime.UtcNow < fim &&
               await relogio.WaitForNextTickAsync(TestContext.Current.CancellationToken))
        {
            var lote = Enumerable.Range(0, 5)
                .Select(_ => cliente.GetAsync($"/api/v1/consolidado/{Dia}",
                    TestContext.Current.CancellationToken))
                .ToArray();
            var respostas = await Task.WhenAll(lote);
            total += respostas.Length;
            sucesso += respostas.Count(r => r.IsSuccessStatusCode);
        }

        var taxa = (double)sucesso / total;
        Assert.True(taxa >= 0.95, $"taxa de sucesso {taxa:P1} em {total} requisições, abaixo de 95%");
        Assert.True(total >= 400, $"apenas {total} requisições executadas");
    }
}
