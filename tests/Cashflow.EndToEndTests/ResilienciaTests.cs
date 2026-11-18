using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Cashflow.Testing;
using Confluent.Kafka;
using Dapper;
using Npgsql;

namespace Cashflow.EndToEndTests;

[Collection("integracao")]
public sealed class ResilienciaTests(PostgresFixture db, KafkaFixture kafka)
{
    [Fact]
    public async Task AC7_evento_com_falha_persistente_vai_para_a_dlt()
    {
        var ct = TestContext.Current.CancellationToken;
        using var fundo = new BackgroundServerFactory(db.ConnectionString, kafka.BootstrapServers,
            lancamentos: false, consolidado: true).Iniciar();

        using var produtor = new ProducerBuilder<string, string>(
            new ProducerConfig { BootstrapServers = kafka.BootstrapServers }).Build();
        await produtor.ProduceAsync(KafkaFixture.Topico,
            new Message<string, string> { Key = "malformado", Value = "{ nao-e-json" }, ct);
        produtor.Flush(TimeSpan.FromSeconds(5));

        using var clienteMetricas = fundo.CreateClient();
        var limite = DateTime.UtcNow.AddSeconds(30);
        var dltConfirmado = false;
        while (DateTime.UtcNow < limite && !dltConfirmado)
        {
            var texto = await clienteMetricas.GetStringAsync("/metrics", ct);
            var linha = texto.Split('\n').FirstOrDefault(l =>
                l.StartsWith("cashflow_dlt_events_total ") || l.StartsWith("cashflow_dlt_events_total{"));

            dltConfirmado = linha is not null
                            && double.TryParse(linha.Split(' ', StringSplitOptions.RemoveEmptyEntries).Last(),
                                NumberStyles.Float, CultureInfo.InvariantCulture, out var valor)
                            && valor > 0;

            if (!dltConfirmado)
                await Task.Delay(500, ct);
        }

        Assert.True(dltConfirmado, "o evento não chegou à DLT");
    }

    /// <summary>
    /// Sem o consolidado (nem a API nem o consumidor), os lançamentos seguem aceitando e o outbox
    /// publica; quando o consolidado volta, o consumidor alcança.
    /// </summary>
    [Fact]
    public async Task AC8_lancamentos_segue_de_pe_com_o_consolidado_fora()
    {
        var comerciante = Guid.NewGuid();
        var ct = TestContext.Current.CancellationToken;

        using var lancamentosApi = WebApiFactory.SoLancamentos(db.ConnectionString, Topologia.Agora);
        using var publicador = new BackgroundServerFactory(db.ConnectionString, kafka.BootstrapServers,
            lancamentos: true, consolidado: false).Iniciar();
        using var cliente = lancamentosApi.CreateClient()
            .Autenticado(comerciante, "lancamentos.write", Guid.NewGuid().ToString());

        var resposta = await cliente.PostAsJsonAsync("/api/v1/lancamentos",
            new { valor = 30.00m, tipo = "credito" }, ct);
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        DateTime? publicado = null;
        var limite = DateTime.UtcNow.AddSeconds(15);
        while (publicado is null && DateTime.UtcNow < limite)
        {
            await using var conn = new NpgsqlConnection(db.ConnectionString);
            publicado = await conn.ExecuteScalarAsync<DateTime?>("""
                SELECT publicado_em FROM outbox_event
                WHERE payload->'payload'->>'comercianteId' = @m
                ORDER BY criado_em DESC LIMIT 1
                """, new { m = comerciante.ToString() });
            if (publicado is null)
                await Task.Delay(250, ct);
        }
        Assert.NotNull(publicado);

        using var consumidor = new BackgroundServerFactory(db.ConnectionString, kafka.BootstrapServers,
            lancamentos: false, consolidado: true).Iniciar();
        using var consolidadoApi = WebApiFactory.SoConsolidado(db.ConnectionString);
        using var clienteConsolidado = consolidadoApi.CreateClient().Autenticado(comerciante, "consolidado.read");

        Assert.NotNull(await clienteConsolidado.EsperarConsolidadoAsync(Topologia.Dia,
            j => j.GetProperty("saldoDia").GetDecimal() == 30.00m, TimeSpan.FromSeconds(40)));
    }
}
