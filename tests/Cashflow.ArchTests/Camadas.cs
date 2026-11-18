using System.Reflection;
using Cashflow.Application;
using Cashflow.Contracts;
using Cashflow.CrossCuttingConcerns.DateTimes;
using Cashflow.Domain.Entities;
using Cashflow.Infrastructure;
using Cashflow.Modules.Consolidado.Entities;
using Cashflow.Modules.Lancamentos.Entities;
using NetArchTest.Rules;

namespace Cashflow.ArchTests;

/// <summary>
/// A separação deixa de ser combinada e vira teste: os compartilhados apontam para dentro
/// e não conhecem módulo; os módulos podem usar os compartilhados; os hosts são as únicas
/// composition roots.
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
    public void CrossCuttingConcerns_nao_conhece_nada_acima() =>
        SemDependencia(typeof(IDateTimeProvider).Assembly,
            "Cashflow.Domain", "Cashflow.Application", "Cashflow.Infrastructure",
            "Cashflow.Modules", "Cashflow.Lancamentos.Api", "Cashflow.Consolidado.Api");

    [Fact]
    public void Domain_nao_conhece_aplicacao_infraestrutura_nem_modulos() =>
        SemDependencia(typeof(Entity<>).Assembly,
            "Cashflow.Application", "Cashflow.Infrastructure",
            "Cashflow.Modules", "Cashflow.Lancamentos.Api", "Cashflow.Consolidado.Api");

    [Fact]
    public void Application_nao_conhece_infraestrutura_nem_modulos() =>
        SemDependencia(typeof(Dispatcher).Assembly,
            "Cashflow.Infrastructure",
            "Cashflow.Modules", "Cashflow.Lancamentos.Api", "Cashflow.Consolidado.Api");

    [Fact]
    public void Contracts_nao_conhece_dominio_infraestrutura_nem_modulos() =>
        SemDependencia(typeof(CashflowEvent<>).Assembly,
            "Cashflow.Domain", "Cashflow.Infrastructure",
            "Cashflow.Modules", "Cashflow.Lancamentos.Api", "Cashflow.Consolidado.Api");

    [Fact]
    public void Infrastructure_nao_conhece_modulos_nem_hosts() =>
        SemDependencia(typeof(CashflowInfrastructure).Assembly,
            "Cashflow.Modules", "Cashflow.Lancamentos.Api", "Cashflow.Consolidado.Api");

    [Fact]
    public void Modulo_de_lancamentos_nao_conhece_o_host_nem_o_outro_modulo() =>
        SemDependencia(typeof(Lancamento).Assembly,
            "Cashflow.Lancamentos.Api", "Cashflow.Modules.Consolidado", "Cashflow.Consolidado.Api");

    [Fact]
    public void Modulo_do_consolidado_nao_conhece_o_host_nem_o_outro_modulo() =>
        SemDependencia(typeof(ConsolidadoDiario).Assembly,
            "Cashflow.Consolidado.Api", "Cashflow.Modules.Lancamentos", "Cashflow.Lancamentos.Api");

    [Fact]
    public void Os_dois_modulos_convivem_em_assemblies_distintos() =>
        Assert.NotEqual(typeof(Lancamento).Assembly, typeof(ConsolidadoDiario).Assembly);
}
