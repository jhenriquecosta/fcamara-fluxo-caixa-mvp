using Cashflow.Blazor.Modules.Consolidado.Services;
using Cashflow.Blazor.Modules.Core.Authorization;
using Cashflow.Blazor.Modules.Infraestrutura.Services;
using Cashflow.Blazor.Modules.Lancamentos.Services;

namespace Cashflow.Terminal;

/// <summary>
/// O que a sessão do terminal carrega: o perfil escolhido no início e os clientes HTTP que as duas
/// UIs compartilham (PRD-0004, "um cliente, duas UIs").
/// </summary>
public sealed class Sessao
{
    /// <summary>O perfil escolhido no início: <c>operador</c> ou <c>consulta</c>.</summary>
    public required string Perfil { get; init; }

    /// <summary>O cliente do serviço de Lançamentos.</summary>
    public required LancamentoService Lancamentos { get; init; }

    /// <summary>O cliente do serviço de Consolidado.</summary>
    public required ConsolidadoService Consolidado { get; init; }

    /// <summary>O cliente de saúde dos serviços.</summary>
    public required SaudeService Saude { get; init; }

    /// <summary>O cliente de métricas do Prometheus.</summary>
    public required PrometheusService Prometheus { get; init; }

    /// <summary>O cliente de logs do Elasticsearch.</summary>
    public required LogsService Logs { get; init; }

    /// <summary>O relógio que define o "hoje" do dia de negócio.</summary>
    public required TimeProvider Relogio { get; init; }

    /// <summary>Só o operador lança e estorna; quem garante o resto é a API, pelos escopos do token.</summary>
    public bool PodeLancar => Perfil == Perfis.Operador;
}
