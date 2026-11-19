# Convenção de Logging — fcamara-fluxo-caixa-mvp

**Estabelecida em:** 2026-11-18
**Stack:** .NET 10 (Serilog + OpenTelemetry)
**Serviços:** Lançamentos · Consolidado

---

## Formato

JSON Lines — o Serilog escreve o **Compact JSON** (`@t`, `@l`, `@mt`) e os enrichers acrescentam `service`/`env` e a correlação. O mesmo arquivo alimenta o Elasticsearch (local) e o CloudWatch Logs (alvo): nada é reescrito para trocar de destino. O sink do Elasticsearch acrescenta o `@timestamp`, que o Kibana usa como campo de tempo.

```json
{"@t":"2026-11-18T21:26:14.554Z","@l":"Information","@mt":"lançamento registrado {LancamentoId} {ComercianteHash} {Tipo} {Valor}","SourceContext":"Cashflow.Modules.Lancamentos.Commands.RegistrarLancamentoCommandHandler","service":"lancamentos","env":"Development","request_id":"a1b2c3","correlation_id":"4bf92f3577b34da6","trace_id":"4bf92f3577b34da6","LancamentoId":"...","ComercianteHash":"...","Tipo":"debito","Valor":100.00}
```

## Campos

| Campo | Obrigatório | Descrição |
|---|---|---|
| `@t` | sim | ISO 8601 com milissegundos, UTC (timestamp do Compact JSON) |
| `@l` | sim | nível: Information · Warning · Error · Fatal |
| `@mt` | sim | template da mensagem, sem dado sensível |
| `SourceContext` | sim | origem (classe/serviço) |
| `service` | sim | `lancamentos` ou `consolidado` (enricher) |
| `env` | sim | o ambiente do host (enricher) |
| `request_id`, `correlation_id`, `trace_id` | em requisição HTTP | o middleware aceita/gera o `X-Request-Id` e propaga o trace W3C |
| `ComercianteHash` | quando houver negócio | hash do `comercianteId`; nunca o id cru |
| `LancamentoId`, `EventId` | quando houver | identificadores de negócio e do evento |
| `@timestamp` | no Elasticsearch | o sink acrescenta; é o campo de tempo do Kibana |

Os campos de negócio aparecem com o nome da propriedade do template (PascalCase); a convenção vale para o conteúdo, não para a capitalização.

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
| `db_pool_saturation` | conexões em uso sobre o máximo do pool — **não exposta pelo driver neste MVP**; no alvo vem das métricas do RDS no CloudWatch |
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
