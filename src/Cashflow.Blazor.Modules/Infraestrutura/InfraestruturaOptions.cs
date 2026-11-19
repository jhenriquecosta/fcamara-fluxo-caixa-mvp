namespace Cashflow.Blazor.Modules.Infraestrutura;

/// <summary>
/// Onde estão os serviços de operação que as UIs consultam (ADR-0011). As URLs internas são as que
/// o servidor alcança (rede do compose); os links são os que o navegador do usuário abre.
/// </summary>
public sealed class InfraestruturaOptions
{
    /// <summary>A seção de configuração.</summary>
    public const string Secao = "Infraestrutura";

    /// <summary>Liga o painel. Só em ambiente de demonstração: ele expõe logs a quem tem sessão.</summary>
    public bool Enabled { get; set; }

    /// <summary>A API HTTP do Prometheus, vista do servidor.</summary>
    public string PrometheusUrl { get; set; } = "http://localhost:9090";

    /// <summary>O Elasticsearch dos logs, visto do servidor.</summary>
    public string ElasticsearchUrl { get; set; } = "http://localhost:9200";

    /// <summary>Os serviços cuja saúde o painel confere, na ordem em que aparecem.</summary>
    public List<ServicoMonitorado> Servicos { get; set; } = new();

    /// <summary>Atalhos que o navegador abre: Grafana, Kibana, Prometheus.</summary>
    public List<AtalhoExterno> Atalhos { get; set; } = new();
}

/// <summary>Um serviço com endpoint de saúde.</summary>
public sealed class ServicoMonitorado
{
    /// <summary>O nome exibido.</summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>O papel do serviço na solução, em poucas palavras.</summary>
    public string Papel { get; set; } = string.Empty;

    /// <summary>A URL que responde 2xx quando o serviço está de pé.</summary>
    public string HealthUrl { get; set; } = string.Empty;
}

/// <summary>Um link para uma ferramenta de operação.</summary>
public sealed class AtalhoExterno
{
    /// <summary>O nome exibido.</summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>O que se vê lá.</summary>
    public string Descricao { get; set; } = string.Empty;

    /// <summary>A URL pública.</summary>
    public string Url { get; set; } = string.Empty;
}
