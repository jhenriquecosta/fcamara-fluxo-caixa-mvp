# Research #0003 — Fontes de dados da experiência de demonstração

**Data:** 2026-11-19 · **Task:** 0011 · **Método:** stack local de pé (`docker compose -p cashflow-web`), lida diretamente. Fonte primária, sem web.

## 1. Quais séries o Prometheus tem?

`GET /api/v1/label/__name__/values`, com dois lançamentos feitos, devolveu: `cashflow_consumer_lag`, `cashflow_lancamentos_total{resultado}`, `cashflow_outbox_pending_age_seconds`, `http_server_request_duration_seconds_{bucket,count,sum}` e `up` por `job` (`lancamentos`, `consolidado`, `background-server`, `prometheus`). `cashflow_dlt_events_total` só aparece depois do primeiro evento em DLT, então a ausência dele significa zero e não erro. As expressões do painel são as mesmas de `observability/prometheus/rules/*.yml`.

## 2. Qual o formato do log no Elasticsearch?

Índice diário `cashflow-logs-yyyy.MM.dd` (sink do Serilog). O documento traz `@timestamp`, `level`, `message`, `messageTemplate` e `fields.{service, SourceContext, RequestPath, StatusCode, ...}`. Os acessos a `/metrics` (scrape a cada 15 s) e `/health` são a maior parte do volume, então a consulta do painel os exclui por `fields.RequestPath`.

## 3. Spectre.Console

Biblioteca de console para .NET com `Table`, `Panel`, `SelectionPrompt`, `Status` e `Live`. O pacote `Spectre.Console.Testing` oferece um `TestConsole` para afirmar a saída. Uso apenas a API estável de prompts e renderização. A versão é a mais recente disponível no NuGet no momento da restauração e fica pinada no `.csproj`.
