using Cashflow.Blazor.Modules.Core.Services;

namespace Cashflow.ArchTests;

/// <summary>
/// O frontend é mais um cliente da API (ADR-0010): as telas só conhecem o contrato HTTP. Se o RCL
/// referenciasse um módulo ou o domínio, um refactor interno quebraria a UI, e a UI poderia pular a
/// autorização da API.
/// </summary>
public sealed class Frontend
{
    [Fact]
    public void Blazor_Modules_so_conhece_o_contrato_http() =>
        Camadas.SemDependencia(typeof(HttpService).Assembly,
            "Cashflow.Modules", "Cashflow.Domain", "Cashflow.Application", "Cashflow.Infrastructure",
            "Cashflow.Contracts", "Cashflow.WebAPI", "Cashflow.BackgroundServer", "Cashflow.BlazorServerSide",
            "Npgsql", "Dapper", "Confluent.Kafka");

    [Fact]
    public void Nenhum_modulo_nem_host_da_api_conhece_o_frontend()
    {
        foreach (var assembly in new[]
                 {
                     typeof(Cashflow.Modules.Lancamentos.Entities.Lancamento).Assembly,
                     typeof(Cashflow.Modules.Consolidado.Entities.ConsolidadoDiario).Assembly,
                     typeof(Cashflow.WebAPI.WebApiEntryPoint).Assembly
                 })
            Camadas.SemDependencia(assembly, "Cashflow.Blazor", "Cashflow.BlazorServerSide");
    }
}
