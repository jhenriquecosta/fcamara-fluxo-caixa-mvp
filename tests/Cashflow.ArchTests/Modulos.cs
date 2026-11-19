using System.Reflection;
using Cashflow.Application;
using Cashflow.Modules.Consolidado.Entities;
using Cashflow.Modules.Lancamentos.Entities;
using NetArchTest.Rules;

namespace Cashflow.ArchTests;

/// <summary>
/// O mecanismo da referência, não só os nomes das pastas: todo caso de uso é um <see cref="ICommand"/>
/// ou <see cref="IQuery{TResult}"/> com o seu handler, e quem o aciona passa pelo <see cref="Dispatcher"/>.
/// </summary>
public sealed class Modulos
{
    public static TheoryData<string> Assemblies => new()
    {
        typeof(Lancamento).Assembly.GetName().Name!,
        typeof(ConsolidadoDiario).Assembly.GetName().Name!
    };

    private static Assembly Carregar(string nome) => Assembly.Load(nome);

    private static IEnumerable<Type> Concretos(Assembly assembly) =>
        assembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false } && !t.IsNested);

    private static bool Implementa(Type tipo, Type generico) =>
        tipo.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == generico);

    private static Type[] Handlers(Assembly assembly) =>
        Concretos(assembly)
            .Where(t => Implementa(t, typeof(ICommandHandler<>)) || Implementa(t, typeof(IQueryHandler<,>)))
            .ToArray();

    [Theory]
    [MemberData(nameof(Assemblies))]
    public void Todo_comando_tem_handler_e_mora_em_Commands(string nome)
    {
        var assembly = Carregar(nome);
        foreach (var comando in Concretos(assembly).Where(t => typeof(ICommand).IsAssignableFrom(t)))
        {
            Assert.EndsWith(".Commands", comando.Namespace);
            Assert.Contains(Handlers(assembly), h =>
                h.GetInterfaces().Contains(typeof(ICommandHandler<>).MakeGenericType(comando)));
        }
    }

    [Theory]
    [MemberData(nameof(Assemblies))]
    public void Toda_consulta_tem_handler_e_mora_em_Queries(string nome)
    {
        var assembly = Carregar(nome);
        foreach (var consulta in Concretos(assembly).Where(t => Implementa(t, typeof(IQuery<>))))
        {
            Assert.EndsWith(".Queries", consulta.Namespace);
            Assert.Contains(Handlers(assembly), h => Implementa(h, typeof(IQueryHandler<,>))
                && h.GetInterfaces().Any(i => i.IsGenericType && i.GenericTypeArguments[0] == consulta));
        }
    }

    [Theory]
    [MemberData(nameof(Assemblies))]
    public void Classe_chamada_Handler_implementa_a_interface_de_handler(string nome)
    {
        var soltos = Concretos(Carregar(nome))
            .Where(t => t.Name.EndsWith("Handler", StringComparison.Ordinal))
            .Where(t => !Implementa(t, typeof(ICommandHandler<>))
                        && !Implementa(t, typeof(IQueryHandler<,>))
                        && !Implementa(t, typeof(Domain.Events.IDomainEventHandler<>)))
            .Select(t => t.FullName)
            .ToList();

        Assert.True(soltos.Count == 0, $"handlers fora do mecanismo: {string.Join(", ", soltos)}");
    }

    /// <summary>Endpoints, workers e consumers acionam o caso de uso pelo Dispatcher, nunca pelo handler concreto.</summary>
    [Theory]
    [MemberData(nameof(Assemblies))]
    public void Ninguem_alem_do_container_depende_de_um_handler_concreto(string nome)
    {
        var assembly = Carregar(nome);
        var handlers = Handlers(assembly).Select(h => h.FullName!).ToArray();

        var resultado = Types.InAssembly(assembly)
            .That().ResideInNamespaceEndingWith(".Endpoints")
            .Or().ResideInNamespaceEndingWith(".HostedServices")
            .Or().ResideInNamespaceEndingWith(".MessageBusConsumers")
            .Or().ResideInNamespaceEndingWith(".OutBoxEventPublishers")
            .ShouldNot().HaveDependencyOnAny(handlers)
            .GetResult();

        Assert.True(resultado.IsSuccessful,
            string.Join(", ", resultado.FailingTypeNames ?? Array.Empty<string>()));
    }

    [Theory]
    [MemberData(nameof(Assemblies))]
    public void Endpoints_despacham_pelo_Dispatcher(string nome)
    {
        var resultado = Types.InAssembly(Carregar(nome))
            .That().ResideInNamespaceEndingWith(".Endpoints")
            .Should().HaveDependencyOn(typeof(Dispatcher).FullName!)
            .GetResult();

        Assert.True(resultado.IsSuccessful,
            string.Join(", ", resultado.FailingTypeNames ?? Array.Empty<string>()));
    }
}
