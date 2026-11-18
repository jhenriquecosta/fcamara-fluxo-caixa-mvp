using Cashflow.Contracts;
using Microsoft.Extensions.Configuration;

namespace Cashflow.Infrastructure.Messaging;

public sealed class KafkaOptions
{
    public string? BootstrapServers { get; init; }
    public string Topic { get; init; } = Topics.Lancamentos;
    public string DltTopic { get; init; } = Topics.LancamentosDlt;
    public string GroupId { get; init; } = Topics.GrupoConsolidado;

    public bool Habilitado => !string.IsNullOrWhiteSpace(BootstrapServers);

    public static KafkaOptions De(IConfiguration configuration) => new()
    {
        BootstrapServers = configuration["Kafka:BootstrapServers"],
        Topic = configuration["Kafka:Topic"] ?? Topics.Lancamentos,
        DltTopic = configuration["Kafka:DltTopic"] ?? Topics.LancamentosDlt,
        GroupId = configuration["Kafka:GroupId"] ?? Topics.GrupoConsolidado
    };
}
