using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cashflow.Testing;
using Dapper;
using Npgsql;

namespace Cashflow.Modules.Consolidado.IntegrationTests;

/// <summary>A consulta do consolidado pela API, contra um PostgreSQL real.</summary>
[Collection("integracao")]
public sealed class ConsolidadoTests(PostgresFixture db)
{
    private const string Dia = "2026-11-17";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Dia_sem_movimento_responde_200_com_saldo_zero_e_o_acumulado_do_ultimo_dia_anterior()
    {
        // Arrange: o comerciante fechou o dia 15 com 80 e não lançou nada desde então
        var comerciante = Guid.NewGuid();
        await using (var conn = new NpgsqlConnection(db.ConnectionString))
        {
            await conn.ExecuteAsync("""
                INSERT INTO consolidado_diario (comerciante_id, data, saldo_dia, saldo_acumulado)
                VALUES (@comerciante, DATE '2026-11-15', 80, 80)
                """, new { comerciante });
        }

        using var api = WebApiFactory.SoConsolidado(db.ConnectionString);
        using var cliente = api.CreateClient().Autenticado(comerciante, "consolidado.read");

        // Act
        var consolidado = await cliente.GetFromJsonAsync<JsonElement>($"/api/v1/consolidado/{Dia}", Ct);

        // Assert
        Assert.Equal(0m, consolidado.GetProperty("saldoDia").GetDecimal());
        Assert.Equal(80m, consolidado.GetProperty("saldoAnterior").GetDecimal());
        Assert.Equal(80m, consolidado.GetProperty("saldoAcumulado").GetDecimal());
    }

    [Fact]
    public async Task Data_fora_do_formato_yyyy_MM_dd_responde_400()
    {
        using var api = WebApiFactory.SoConsolidado(db.ConnectionString);
        using var cliente = api.CreateClient().Autenticado(Guid.NewGuid(), "consolidado.read");

        var resposta = await cliente.GetAsync("/api/v1/consolidado/17-11-2026", Ct);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    [Trait("Criterio", "AC8")]
    public async Task Consolidado_api_nao_expoe_a_rota_de_lancamentos()
    {
        using var api = WebApiFactory.SoConsolidado(db.ConnectionString);
        using var cliente = api.CreateClient().Autenticado(Guid.NewGuid(), "lancamentos.write", Guid.NewGuid().ToString());

        var resposta = await cliente.PostAsJsonAsync("/api/v1/lancamentos", new { valor = 10.00m, tipo = "debito" }, Ct);

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    /// <summary>
    /// O requisito não funcional: 50 req/s no consolidado com no máximo 5% de falha. Dispara 5 requisições
    /// a cada 100 ms por 10 s e mede quantas deram certo.
    /// </summary>
    [Fact]
    [Trait("Criterio", "AC9")]
    public async Task Consolidado_sustenta_50_requisicoes_por_segundo_com_ate_5_por_cento_de_falha()
    {
        // Arrange
        using var api = WebApiFactory.SoConsolidado(db.ConnectionString);
        using var cliente = api.CreateClient().Autenticado(Guid.NewGuid(), "consolidado.read");
        var total = 0;
        var comSucesso = 0;
        var fim = DateTime.UtcNow.AddSeconds(10);

        // Act
        using var aCada100ms = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
        while (DateTime.UtcNow < fim && await aCada100ms.WaitForNextTickAsync(Ct))
        {
            var respostas = await Task.WhenAll(Enumerable.Range(0, 5)
                .Select(_ => cliente.GetAsync($"/api/v1/consolidado/{Dia}", Ct)));
            total += respostas.Length;
            comSucesso += respostas.Count(r => r.IsSuccessStatusCode);
        }

        // Assert
        var taxaDeSucesso = (double)comSucesso / total;
        Assert.True(taxaDeSucesso >= 0.95, $"taxa de sucesso {taxaDeSucesso:P1} em {total} requisições, abaixo de 95%");
        Assert.True(total >= 400, $"apenas {total} requisições executadas; a carga não chegou a 50 req/s");
    }
}
