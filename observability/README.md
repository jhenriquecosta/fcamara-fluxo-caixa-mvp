# Observabilidade — fluxo de caixa

Stack local: **ELK** para logs (Elasticsearch + Kibana, ingestão direta pelo Serilog) e **Prometheus + Grafana** para métricas. **OpenTelemetry** é a camada de coleta nos dois casos — o destino muda entre local e AWS sem reescrever a aplicação.

## Mapeamento local ↔ alvo AWS

| Local (MVP) | Alvo AWS (ADR-010) | Papel |
|---|---|---|
| Elasticsearch + Kibana | CloudWatch Logs + Logs Insights (ou OpenSearch Service + Dashboards) | Busca e análise de logs |
| Serilog com sink Elasticsearch | Serilog/OpenTelemetry com sink CloudWatch Logs | Ingestão dos logs JSON |
| Prometheus | CloudWatch Metrics (via OpenTelemetry Collector / EMF) ou Amazon Managed Service for Prometheus | Métricas |
| Prometheus Alertmanager | CloudWatch Alarms | Alarmes |
| Grafana | CloudWatch Dashboards ou Amazon Managed Grafana | Dashboards |
| OpenTelemetry | OpenTelemetry Collector → CloudWatch | Camada portável de telemetria |

A aplicação emite **OpenTelemetry** e logs JSON; trocar o backend é configuração, não código. É o argumento do diferencial de observabilidade.

## Golden Signals

| Sinal | Lançamentos | Consolidado |
|---|---|---|
| **Latência** | p50/p95/p99 do POST e do estorno | p50/p95/p99 do GET |
| **Tráfego** | requisições/s, lançamentos/s, eventos publicados/s | requisições/s (meta: 50 req/s no pico), eventos consumidos/s |
| **Erros** | 4xx de validação/idempotência, 5xx, 401/403 | 5xx, falhas de consumo, eventos na DLT |
| **Saturação** | pool do banco, backlog do outbox, CPU/memória | lag do consumidor, pool do banco, DLT |

## Estrutura

```
observability/
├── elasticsearch/
│   ├── ilm-policies/cashflow-logs-7d.json
│   └── index-templates/cashflow-logs.json
├── prometheus/
│   ├── prometheus.yml
│   └── rules/cashflow-recording.yml · cashflow-alerts.yml
├── grafana/
│   ├── provisioning/datasources.yaml · dashboards.yaml
│   └── dashboards/cashflow-golden-signals.json
├── slos/cashflow.yml
└── scripts/apply-elk.sh
```

## Como aplicar

1. Suba o compose (fatia 5) — Elasticsearch, Kibana, Prometheus e Grafana sobem com os volumes montados daqui.
2. Rode `observability/scripts/apply-elk.sh` — cria a política de ILM, o template de índice e a data view do Kibana. O script usa `curl` e é idempotente.
3. O Prometheus já lê `prometheus.yml` e as regras montadas; o Grafana provisiona a datasource e o dashboard.
4. Kibana em `http://localhost:5601` (data view `cashflow-logs-*`); Grafana em `http://localhost:3000` (dashboard "Fluxo de caixa — Golden Signals").

## Retenção

- Elasticsearch: 7 dias (ILM `cashflow-logs-7d`), 1 shard, sem réplica no local.
- Prometheus: 7 dias (flag `--storage.tsdb.retention.time=7d` no compose).
- Alvo AWS: 30 dias no CloudWatch Logs (definido no ADR-010).

## Sobre o Logstash

O "L" do ELK fica **opcional** neste MVP: o Serilog escreve direto no Elasticsearch, o que remove um serviço JVM pesado do compose local. Em um ambiente onde o pipeline precise de enriquecimento, filtros ou fan-out, o Logstash entra entre a aplicação e o Elasticsearch sem mudar o formato do log — a convenção JSON Lines já é o que ele consome.
