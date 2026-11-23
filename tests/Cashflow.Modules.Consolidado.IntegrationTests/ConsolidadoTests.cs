using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cashflow.Application;
using Cashflow.Modules.Consolidado.Commands;
using Cashflow.Modules.Consolidado.Entities;
using Cashflow.Testing;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
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

    /// <summary>
    /// Um movimento de D que chega depois de D+1 já aberto — virada da meia-noite com o outbox atrasado ou
    /// reprocessamento da DLT — não é lançamento retroativo: o dia dele é o do registro. Mas o acumulado de
    /// D+1 foi aberto sem ele, então a projeção precisa propagar o efeito para os dias posteriores.
    /// </summary>
    [Fact]
    public async Task Movimento_que_chega_atrasado_propaga_o_efeito_para_o_acumulado_dos_dias_posteriores()
    {
        // Arrange: D+1 já foi projetado com um crédito de 30 antes do débito de 10 em D
        var comerciante = Guid.NewGuid();
        var dia = new DateOnly(2026, 11, 17);
        using var api = WebApiFactory.SoConsolidado(db.ConnectionString);
        await ProjetarAsync(api, new MovimentoDoDia(Guid.NewGuid(), comerciante, "credito", 30m, dia.AddDays(1)));

        // Act: o débito de D chega atrasado
        await ProjetarAsync(api, new MovimentoDoDia(Guid.NewGuid(), comerciante, "debito", 10m, dia));

        // Assert: D+1 abre com o -10 de D e fecha em 20
        using var cliente = api.CreateClient().Autenticado(comerciante, "consolidado.read");
        var diaSeguinte = await cliente.GetFromJsonAsync<JsonElement>(
            $"/api/v1/consolidado/{dia.AddDays(1):yyyy-MM-dd}", Ct);
        Assert.Equal(30m, diaSeguinte.GetProperty("saldoDia").GetDecimal());
        Assert.Equal(-10m, diaSeguinte.GetProperty("saldoAnterior").GetDecimal());
        Assert.Equal(20m, diaSeguinte.GetProperty("saldoAcumulado").GetDecimal());
    }

    private static async Task ProjetarAsync(WebApiFactory api, MovimentoDoDia movimento)
    {
        using var escopo = api.Services.CreateScope();
        await escopo.ServiceProvider.GetRequiredService<Dispatcher>()
            .DispatchAsync(new ProjetarMovimentoCommand { Movimento = movimento }, Ct);
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
