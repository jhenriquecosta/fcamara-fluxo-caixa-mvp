using Cashflow.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;

namespace Cashflow.Infrastructure.Configuration;

public static class CashflowDefaults
{
    public static readonly IReadOnlyDictionary<string, string?> Valores =
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
    /// Entra como o primeiro provedor da configuração: o appsettings, as variáveis de
    /// ambiente e os argumentos da linha de comando continuam tendo precedência.
    /// </summary>
    public static void Aplicar(IConfigurationBuilder builder)
    {
        var fonte = new MemoryConfigurationSource { InitialData = Valores };

        if (builder is ConfigurationManager manager)
            manager.Sources.Insert(0, fonte);
        else
            builder.AddInMemoryCollection(Valores);
    }
}
