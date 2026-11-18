# Observabilidade — fluxo de caixa

*Diferencial do desafio. O stack local é ELK + Prometheus + Grafana; o alvo é CloudWatch. A aplicação emite OpenTelemetry e logs JSON — o destino é configuração, não código. As configurações estão em [`observability/`](../../observability/README.md).*

## Golden Signals

| Sinal | Lançamentos | Consolidado |
|---|---|---|
| **Latência** | p50/p95/p99 do `POST /lancamentos` e do estorno | p50/p95/p99 do `GET /consolidado/{data}` |
| **Tráfego** | requisições/s; lançamentos/s; eventos publicados/s | requisições/s (pico de 50); eventos consumidos/s |
| **Erros** | 4xx de validação e idempotência; 5xx; 401/403 | 5xx; falhas de consumo; eventos na DLT |
| **Saturação** | backlog do outbox (idade do evento pendente) | lag do consumidor; DLT; pool do banco (no alvo) |

## SLOs propostos

As metas vêm do PRD (RNF-3 a RNF-5) e são **propostas a validar** no alvo:

| SLO | Meta | Janela |
|---|---|---|
| Disponibilidade dos lançamentos | 99,9% | 30 dias |
| Disponibilidade do consolidado | 99,5% | 30 dias |
| Latência p95 dos lançamentos | 95% abaixo de 300 ms | 30 dias |
| Latência p95 do consolidado | 95% abaixo de 200 ms | 30 dias |
| Frescor do consolidado | 99% dos eventos aplicados em até 60 s | 30 dias |

O *error budget* e a política de burn rate estão em [`observability/slos/cashflow.yml`](../../observability/slos/cashflow.yml) (14,4× em 1 h → crítico; 6× em 6 h → aviso).

## Alarmes

As regras do Prometheus em [`observability/prometheus/rules/`](../../observability/prometheus/rules/) cobrem:

| Alarme | Condição | Ação esperada |
|---|---|---|
| Taxa de erro alta | 5xx > 5% por 2 min | Ver os logs por `trace_id`; diagnosticar o endpoint |
| Latência alta (por serviço) | p95 acima do alvo por 5 min | Ver pool do banco, carga e o endpoint |
| Idade do outbox | evento pendente > 10 s por 5 min | Ver o broker e o publicador; o lançamento não parou |
| Lag do consumidor | > 1.000 mensagens por 5 min | O consolidado está atrás; ver a saúde do consumidor |
| Evento na DLT | qualquer evento em 5 min | Investigar a falha antes de reprocessar |
| Serviço fora do ar | `up == 0` por 1 min | Ver a task, o health check e o ALB |

No alvo AWS, os mesmos sinais viram **CloudWatch Alarms** sobre as métricas do ADR-010 (idade do outbox, lag, DLT > 0, 5xx, saturação dos pools).

## Logs

A convenção está em [`docs/ultron/agents/logging.md`](../ultron/agents/logging.md). O evento é JSON Lines com `ts`, `level`, `context`, `service`, `env`, `msg`, `correlation_id`, `trace_id`, `request_id` e os campos de negócio (`comerciante_hash`, `lancamento_id`, `event_id`, `tipo`, `valor`, `status`, `duration_ms`). Nunca entram segredo, token ou PII; a `Idempotency-Key` e o corpo só como hash/prefixo.

Local: Elasticsearch com o template e a política de 7 dias (`observability/elasticsearch/`), Kibana para busca. Alvo: CloudWatch Logs com retenção de 30 dias.

## Local ↔ AWS

| Local | Alvo AWS | Papel |
|---|---|---|
| Elasticsearch + Kibana | CloudWatch Logs + Logs Insights (ou OpenSearch + Dashboards) | Logs |
| Prometheus | CloudWatch Metrics (via OTel Collector/EMF) ou Managed Service for Prometheus | Métricas |
| Alertmanager | CloudWatch Alarms | Alarmes |
| Grafana | CloudWatch Dashboards ou Managed Grafana | Dashboards |
| OpenTelemetry | OpenTelemetry Collector | Coleta portável |

## O que o MVP prova e o que fica em aberto

**Provado:** `/metrics` com as métricas de HTTP, do contador de lançamentos, da idade do outbox, do lag e da DLT; o dashboard "Golden Signals" provisionado; as regras de alarme; o teste de carga de 50 req/s com ≥95%; o teste de isolamento de falha.

**Em aberto, declarado:** a saturação do pool **não é exposta pelo driver Npgsql neste MVP** — no alvo ela vem das métricas do RDS no CloudWatch (o alarme correspondente não foi deixado ligado a uma métrica inexistente); os limiares dos alarmes são propostas; a carga foi medida em processo, não com clientes externos; o Kibana tem a data view e a busca, mas o dashboard de logs é operacional, não um entregável fixo.
