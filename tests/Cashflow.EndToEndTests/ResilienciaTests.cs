using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Cashflow.Testing;
using Confluent.Kafka;
using Dapper;
using Npgsql;

namespace Cashflow.EndToEndTests;

/// <summary>O que acontece quando algo dá errado: mensagem envenenada e consolidado fora do ar.</summary>
[Collection("integracao")]
public sealed class ResilienciaTests(PostgresFixture db, KafkaFixture kafka)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    [Trait("Criterio", "AC7")]
    public async Task Mensagem_que_falha_em_todas_as_tentativas_vai_para_a_DLT_sem_travar_o_consumo()
    {
        // Arrange
        using var consumidor = new BackgroundServerFactory(db.ConnectionString, kafka.BootstrapServers,
            lancamentos: false, consolidado: true).Iniciar();

        // Act: um texto que nem é JSON chega ao tópico
        using var produtor = new ProducerBuilder<string, string>(
            new ProducerConfig { BootstrapServers = kafka.BootstrapServers }).Build();
        await produtor.ProduceAsync(KafkaFixture.Topico,
            new Message<string, string> { Key = "malformado", Value = "{ nao-e-json" }, Ct);
        produtor.Flush(TimeSpan.FromSeconds(5));

        // Assert: a métrica da DLT sobe (as tentativas levam ~7 s)
        using var prometheus = consumidor.CreateClient();
        Assert.True(await EsperarMetricaPositivaAsync(prometheus, "cashflow_dlt_events_total", TimeSpan.FromSeconds(30)),
            "o evento não chegou à DLT");
    }

    /// <summary>
    /// Requisito central da arquitetura: o serviço de lançamentos não pode cair porque o consolidado caiu.
    /// Sem a API e sem o consumidor do consolidado, os lançamentos seguem aceitando e o outbox segue
    /// publicando; quando o consolidado volta, o consumidor alcança.
    /// </summary>
    [Fact]
    [Trait("Criterio", "AC8")]
    public async Task Lancamentos_segue_aceitando_com_o_consolidado_fora_e_o_consolidado_alcanca_ao_voltar()
    {
        // Arrange: só a lancamentos-api e o publicador do outbox estão no ar
        var comerciante = Guid.NewGuid();
        using var lancamentosApi = WebApiFactory.SoLancamentos(db.ConnectionString, Topologia.Agora);
        using var publicador = new BackgroundServerFactory(db.ConnectionString, kafka.BootstrapServers,
            lancamentos: true, consolidado: false).Iniciar();
        using var cliente = lancamentosApi.CreateClient()
            .Autenticado(comerciante, "lancamentos.write", Guid.NewGuid().ToString());

        // Act 1: lança com o consolidado fora
        var resposta = await cliente.PostAsJsonAsync("/api/v1/lancamentos", new { valor = 30.00m, tipo = "credito" }, Ct);

        // Assert 1: aceito e publicado no tópico
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.True(await EsperarPublicacaoNoOutboxAsync(comerciante, TimeSpan.FromSeconds(15)),
            "o outbox não publicou com o consolidado fora");

        // Act 2: o consolidado volta
        using var consumidor = new BackgroundServerFactory(db.ConnectionString, kafka.BootstrapServers,
            lancamentos: false, consolidado: true).Iniciar();
        using var consolidadoApi = WebApiFactory.SoConsolidado(db.ConnectionString);
        using var clienteConsolidado = consolidadoApi.CreateClient().Autenticado(comerciante, "consolidado.read");

        // Assert 2: e alcança o que foi lançado enquanto esteve fora
        Assert.NotNull(await clienteConsolidado.EsperarConsolidadoAsync(Topologia.Dia,
            j => j.GetProperty("saldoDia").GetDecimal() == 30.00m, TimeSpan.FromSeconds(40)));
    }

    private async Task<bool> EsperarPublicacaoNoOutboxAsync(Guid comerciante, TimeSpan prazo)
    {
        var limite = DateTime.UtcNow + prazo;
        while (DateTime.UtcNow < limite)
        {
            await using var conn = new NpgsqlConnection(db.ConnectionString);
            var publicadoEm = await conn.ExecuteScalarAsync<DateTime?>("""
                SELECT publicado_em FROM outbox_event
                WHERE payload->'payload'->>'comercianteId' = @comerciante
                ORDER BY criado_em DESC LIMIT 1
                """, new { comerciante = comerciante.ToString() });
            if (publicadoEm is not null)
                return true;
            await Task.Delay(250, Ct);
        }
        return false;
    }

    private static async Task<bool> EsperarMetricaPositivaAsync(HttpClient prometheus, string metrica, TimeSpan prazo)
    {
        var limite = DateTime.UtcNow + prazo;
        while (DateTime.UtcNow < limite)
        {
            var linha = (await prometheus.GetStringAsync("/metrics", Ct)).Split('\n')
                .FirstOrDefault(l => l.StartsWith(metrica + " ") || l.StartsWith(metrica + "{"));

            if (linha is not null
                && double.TryParse(linha.Split(' ', StringSplitOptions.RemoveEmptyEntries).Last(),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out var valor)
                && valor > 0)
                return true;

            await Task.Delay(500, Ct);
        }
        return false;
    }
}
