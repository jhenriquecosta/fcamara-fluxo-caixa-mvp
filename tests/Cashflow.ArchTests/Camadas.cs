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
/// A direção das dependências do ModularMonolith de referência, provada: os compartilhados apontam
/// para dentro e não conhecem módulo; o módulo usa os compartilhados e não conhece o outro módulo
/// nem os hosts; só os hosts compõem.
/// </summary>
public sealed class Camadas
{
    private static readonly string[] Hosts = { "Cashflow.WebAPI", "Cashflow.BackgroundServer", "Cashflow.Migrator" };

    internal static void SemDependencia(Assembly assembly, params string[] proibidos)
    {
        var resultado = Types.InAssembly(assembly).ShouldNot().HaveDependencyOnAny(proibidos).GetResult();

        Assert.True(resultado.IsSuccessful,
            string.Join(", ", resultado.FailingTypeNames ?? Array.Empty<string>()));
    }

    [Fact]
    public void CrossCuttingConcerns_nao_conhece_nenhum_outro_projeto() =>
        SemDependencia(typeof(IDateTimeProvider).Assembly,
            [.. Hosts, "Cashflow.Domain", "Cashflow.Application", "Cashflow.Contracts", "Cashflow.Infrastructure", "Cashflow.Modules"]);

    [Fact]
    public void Domain_conhece_no_maximo_o_CrossCuttingConcerns() =>
        SemDependencia(typeof(Entity<>).Assembly,
            [.. Hosts, "Cashflow.Application", "Cashflow.Contracts", "Cashflow.Infrastructure", "Cashflow.Modules"]);

    [Fact]
    public void Application_nao_conhece_infraestrutura_contratos_nem_modulos() =>
        SemDependencia(typeof(Dispatcher).Assembly,
            [.. Hosts, "Cashflow.Infrastructure", "Cashflow.Contracts", "Cashflow.Modules", "Npgsql", "Confluent.Kafka"]);

    [Fact]
    public void Contracts_nao_depende_de_nada() =>
        SemDependencia(typeof(CashflowEvent<>).Assembly,
            [.. Hosts, "Cashflow.CrossCuttingConcerns", "Cashflow.Domain", "Cashflow.Application", "Cashflow.Infrastructure", "Cashflow.Modules"]);

    [Fact]
    public void Infrastructure_nao_conhece_modulos_nem_hosts() =>
        SemDependencia(typeof(CashflowInfrastructure).Assembly, [.. Hosts, "Cashflow.Modules"]);

    [Fact]
    public void Modulo_de_lancamentos_nao_conhece_o_outro_modulo_nem_os_hosts() =>
        SemDependencia(typeof(Lancamento).Assembly, [.. Hosts, "Cashflow.Modules.Consolidado"]);

    [Fact]
    public void Modulo_do_consolidado_nao_conhece_o_outro_modulo_nem_os_hosts() =>
        SemDependencia(typeof(ConsolidadoDiario).Assembly, [.. Hosts, "Cashflow.Modules.Lancamentos"]);

    [Fact]
    public void Modulos_falam_com_o_broker_so_pelo_IMessageBus()
    {
        SemDependencia(typeof(Lancamento).Assembly, "Confluent.Kafka");
        SemDependencia(typeof(ConsolidadoDiario).Assembly, "Confluent.Kafka");
    }
}
