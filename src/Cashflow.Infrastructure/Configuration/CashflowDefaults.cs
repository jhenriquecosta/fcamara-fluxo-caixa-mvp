using Cashflow.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;

namespace Cashflow.Infrastructure.Configuration;

/// <summary>
/// Os valores que todo host usa se ninguém disser outra coisa: emissor e audiência do token, fuso do
/// negócio, níveis de log, broker e tópicos. Ficam aqui uma vez, e não copiados em cada <c>appsettings</c>.
/// </summary>
public static class CashflowDefaults
{
    /// <summary>
    /// Os padrões. A <c>Auth:SigningKey</c> daqui só serve ao ambiente local: em produção ela vem de
    /// variável de ambiente ou cofre, que têm precedência.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string?> Values =
        new Dictionary<string, string?>
        {
            ["Auth:Issuer"] = "cashflow-local",
            ["Auth:Audience"] = "cashflow-api",
            ["Auth:SigningKey"] = "dev-only-signing-key-change-me-32-chars-min",
            ["TimeZone:Business"] = "America/Sao_Paulo",
            ["Serilog:MinimumLevel:Default"] = "Information",
            ["Serilog:MinimumLevel:Override:Microsoft"] = "Warning",
            ["Serilog:MinimumLevel:Override:Microsoft.AspNetCore"] = "Warning",
            ["MessageBroker:Provider"] = "Kafka",
            ["MessageBroker:Kafka:GroupId"] = Topics.GrupoConsolidado,
            [$"MessageBroker:Kafka:Topics:{nameof(LancamentoEventPayload)}"] = Topics.Lancamentos,
            [$"MessageBroker:Kafka:DeadLetterTopics:{nameof(LancamentoEventPayload)}"] = Topics.LancamentosDlt
        };

    /// <summary>
    /// Põe os padrões como a primeira fonte da configuração, a de menor prioridade: o appsettings, as
    /// variáveis de ambiente e a linha de comando continuam valendo por cima deles.
    /// </summary>
    public static void RegisterAsLowestPrioritySource(IConfigurationBuilder builder)
    {
        var source = new MemoryConfigurationSource { InitialData = Values };

        if (builder is ConfigurationManager manager)
            manager.Sources.Insert(0, source);
        else
            builder.AddInMemoryCollection(Values);
    }
}
