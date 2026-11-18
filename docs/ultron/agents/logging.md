# Convenção de Logging — fcamara-fluxo-caixa-mvp

**Estabelecida em:** 2026-11-18
**Stack:** .NET 10 (Serilog + OpenTelemetry)
**Serviços:** Lançamentos · Consolidado

---

## Formato

JSON Lines — um evento por linha, em `logs/{serviço}.log` e no console. O mesmo formato alimenta o Elasticsearch (local) e o CloudWatch Logs (alvo AWS): nada é reescrito para trocar de destino.

```json
{"ts":"2026-11-18T20:40:00.123Z","level":"INFO","context":"Lancamentos.Api","service":"lancamentos","env":"dev","msg":"lançamento registrado","correlation_id":"4bf92f3577b34da6","trace_id":"4bf92f3577b34da6","request_id":"a1b2c3","lancamento_id":"...","comerciante_hash":"...","tipo":"debito","valor":100.00,"status":201,"duration_ms":18}
```

## Campos

| Campo | Obrigatório | Descrição |
|---|---|---|
| `ts` | sim | ISO 8601 com milissegundos, UTC |
| `level` | sim | DEBUG · INFO · WARN · ERROR · FATAL |
| `context` | sim | módulo/classe/serviço de origem |
| `service` | sim | `lancamentos` ou `consolidado` |
| `env` | sim | `dev`, `test` ou o ambiente do alvo |
| `msg` | sim | mensagem curta, sem dado sensível |
| `correlation_id` | sim | rastreia a operação entre os serviços (equivale ao `trace_id` W3C quando houver) |
| `trace_id` / `span_id` | quando houver OpenTelemetry | propagação W3C |
| `request_id` | sim em requisição HTTP | gerado ou recebido no header `X-Request-Id`, devolvido na resposta |
| `comerciante_hash` | quando houver negócio | hash do `comercianteId`; nunca o id cru em log compartilhado |
| `lancamento_id`, `event_id` | quando houver | identificadores de negócio e do evento |
| `duration_ms`, `status` | em requisição e consumo | duração e código HTTP/resultado |
| `dlt` | em falha persistente | `true` quando o evento vai à DLT |

## Destino e ciclo de vida

- **Local:** `logs/{serviço}.log` com rotação diária e por tamanho (100 MB), retenção de 14 dias; console em JSON para o compose.
- **Busca local (ELK):** Elasticsearch com índice por serviço (`logs-lancamentos-*`, `logs-consolidado-*`), retenção de 7 dias no MVP; Kibana para busca e dashboards.
- **Alvo AWS:** CloudWatch Logs, um log group por serviço, retenção de 30 dias; as métricas sobem para CloudWatch Metrics e os alarmes do ADR-010.
- **Nível padrão:** INFO. DEBUG só por variável de ambiente (`Serilog__MinimumLevel=Debug`), nunca ligado por padrão em produção.

## Métricas (OpenTelemetry → Prometheus no local, CloudWatch no alvo)

O serviço expõe `/metrics` no formato Prometheus. Métricas obrigatórias, conforme o ADR-010:

| Métrica | O que mede |
|---|---|
| `outbox_pending_age_seconds` | idade do evento mais antigo ainda não publicado |
| `outbox_published_total` | eventos publicados |
| `consumer_lag` | lag do consumidor por partição |
| `dlt_events_total` | eventos enviados à DLT |
| `http_server_5xx_total` / `http_server_requests_total` | taxa de 5xx sobre o total |
| `db_pool_saturation` | conexões em uso sobre o máximo do pool |
| `lancamentos_total{resultado}` | registrados, idempotentes e conflitos |

## Eventos que sempre devem ser logados

- Lançamento registrado: `lancamento_id`, `comerciante_hash`, `tipo`, `valor`, `status`.
- Idempotência: acerto (mesma resposta), conflito de payload (`422`) e concorrência (`409`).
- Estorno registrado: `lancamento_id` do estorno e `estorno_de`.
- Evento publicado pelo outbox e evento consumido: `event_id`, `comerciante_hash`, resultado.
- Evento repetido ignorado no consumo.
- Falha de consumo, tentativas e envio à DLT.
- Autenticação negada (`401`) e autorização negada (`403`).
- Início e fim de cada requisição HTTP, via log de request do ASP.NET Core.

## Nunca logar

Senhas, tokens, chaves, cabeçalhos de autenticação, dados pessoais sem necessidade de negócio, payloads inteiros de requisição. A `Idempotency-Key` e o corpo do lançamento entram no log apenas como hash/prefixo quando necessário para depuração — nunca crus.

---

## Decisão de escalada para observabilidade completa

- [x] Vai a produção com SLO/SLA declarado? — os RNFs do desafio declaram metas de disponibilidade e desempenho.
- [x] Múltiplos serviços que precisam de correlação? — Lançamentos e Consolidado.
- [x] Projeto já tem stack de observability declarado? — o alvo AWS (CloudWatch) e a decisão do Operador de usar ELK + Prometheus localmente.

**Status:** escalado para `/ultron-observability` em 18/11/2026 — stack local ELK + Prometheus + Grafana; alvo AWS CloudWatch, com o mapeamento entre os dois no documento de observabilidade.

---

*Convenção estabelecida por `/ultron-logging`. Consultada como precondição pelo `/ultron-coder`
em trabalho feature/hotfix/refactor.*
