using System.Reflection;
using Cashflow.Common;
using Cashflow.Consolidado.Domain;
using Cashflow.Lancamentos.Domain;
using NetArchTest.Rules;

namespace Cashflow.ArchTests;

/// <summary>
/// A separação deixa de ser combinada e vira teste: a dependência aponta para dentro.
/// </summary>
public sealed class Camadas
{
    private static void SemDependencia(Assembly assembly, params string[] proibidos)
    {
        var resultado = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny(proibidos)
            .GetResult();

        Assert.True(resultado.IsSuccessful,
            string.Join(", ", resultado.FailingTypeNames ?? Array.Empty<string>()));
    }

    [Fact]
    public void Dominio_de_lancamentos_nao_conhece_as_camadas_externas() =>
        SemDependencia(typeof(Lancamento).Assembly,
            "Cashflow.Lancamentos.Application",
            "Cashflow.Lancamentos.Infrastructure",
            "Cashflow.Lancamentos.Api");

    [Fact]
    public void Aplicacao_de_lancamentos_nao_conhece_infraestrutura_nem_api() =>
        SemDependencia(typeof(Cashflow.Lancamentos.Application.LancamentoCorpo).Assembly,
            "Cashflow.Lancamentos.Infrastructure",
            "Cashflow.Lancamentos.Api");

    [Fact]
    public void Infraestrutura_de_lancamentos_nao_conhece_a_api() =>
        SemDependencia(typeof(Cashflow.Lancamentos.Infrastructure.Persistence.LancamentoRepository).Assembly,
            "Cashflow.Lancamentos.Api");

    [Fact]
    public void Dominio_do_consolidado_nao_conhece_as_camadas_externas() =>
        SemDependencia(typeof(ConsolidadoDiario).Assembly,
            "Cashflow.Consolidado.Application",
            "Cashflow.Consolidado.Infrastructure",
            "Cashflow.Consolidado.Api");

    [Fact]
    public void Aplicacao_do_consolidado_nao_conhece_infraestrutura_nem_api() =>
        SemDependencia(typeof(Cashflow.Consolidado.Application.ConsultarConsolidadoHandler).Assembly,
            "Cashflow.Consolidado.Infrastructure",
            "Cashflow.Consolidado.Api");

    [Fact]
    public void Lib_comum_nao_conhece_nenhum_servico() =>
        SemDependencia(typeof(CashflowCommon).Assembly,
            "Cashflow.Lancamentos",
            "Cashflow.Consolidado");

    [Fact]
    public void Contratos_nao_conhecem_nenhum_servico() =>
        SemDependencia(typeof(Cashflow.Contracts.CashflowEvent<>).Assembly,
            "Cashflow.Lancamentos",
            "Cashflow.Consolidado",
            "Cashflow.Common");
}
