using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cashflow.Testing;
using Dapper;
using Npgsql;

namespace Cashflow.Modules.Consolidado.IntegrationTests;

[Collection("integracao")]
public sealed class ConsolidadoTests(PostgresFixture db)
{
    private const string Dia = "2026-11-17";

    [Fact]
    public async Task Dia_sem_movimento_responde_zero_com_o_acumulado_do_dia_anterior()
    {
        var comerciante = Guid.NewGuid();
        await using (var conn = new NpgsqlConnection(db.ConnectionString))
        {
            await conn.ExecuteAsync("""
                INSERT INTO consolidado_diario (comerciante_id, data, saldo_dia, saldo_acumulado)
                VALUES (@m, DATE '2026-11-15', 80, 80)
                """, new { m = comerciante });
        }

        using var factory = WebApiFactory.SoConsolidado(db.ConnectionString);
        using var client = factory.CreateClient().Autenticado(comerciante, "consolidado.read");

        var json = await client.GetFromJsonAsync<JsonElement>($"/api/v1/consolidado/{Dia}",
            TestContext.Current.CancellationToken);

        Assert.Equal(0m, json.GetProperty("saldoDia").GetDecimal());
        Assert.Equal(80m, json.GetProperty("saldoAnterior").GetDecimal());
        Assert.Equal(80m, json.GetProperty("saldoAcumulado").GetDecimal());
    }

    [Fact]
    public async Task Data_invalida_responde_400()
    {
        using var factory = WebApiFactory.SoConsolidado(db.ConnectionString);
        using var client = factory.CreateClient().Autenticado(Guid.NewGuid(), "consolidado.read");

        var response = await client.GetAsync("/api/v1/consolidado/17-11-2026", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Consolidado_api_nao_expoe_os_lancamentos()
    {
        using var factory = WebApiFactory.SoConsolidado(db.ConnectionString);
        using var client = factory.CreateClient().Autenticado(Guid.NewGuid(), "lancamentos.write", Guid.NewGuid().ToString());

        var response = await client.PostAsJsonAsync("/api/v1/lancamentos",
            new { valor = 10.00m, tipo = "debito" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AC9_consolidado_sustenta_50_requisicoes_por_segundo()
    {
        using var factory = WebApiFactory.SoConsolidado(db.ConnectionString);
        using var cliente = factory.CreateClient().Autenticado(Guid.NewGuid(), "consolidado.read");
        var ct = TestContext.Current.CancellationToken;

        var total = 0;
        var sucesso = 0;
        var fim = DateTime.UtcNow.AddSeconds(10);

        using var relogio = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
        while (DateTime.UtcNow < fim && await relogio.WaitForNextTickAsync(ct))
        {
            var respostas = await Task.WhenAll(Enumerable.Range(0, 5)
                .Select(_ => cliente.GetAsync($"/api/v1/consolidado/{Dia}", ct)));
            total += respostas.Length;
            sucesso += respostas.Count(r => r.IsSuccessStatusCode);
        }

        var taxa = (double)sucesso / total;
        Assert.True(taxa >= 0.95, $"taxa de sucesso {taxa:P1} em {total} requisições, abaixo de 95%");
        Assert.True(total >= 400, $"apenas {total} requisições executadas");
    }
}
