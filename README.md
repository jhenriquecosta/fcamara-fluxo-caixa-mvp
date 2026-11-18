# Fluxo de caixa — MVP (desafio Carrefour / FCamara)

Dois serviços em .NET 10 — **Lançamentos** e **Consolidado** — que registram débitos e créditos com idempotência, corrigem por contra-lançamento e derivam o saldo diário por evento. A arquitetura-alvo é AWS (desenhada no ADR-010); o que roda aqui é o MVP local, com PostgreSQL, Kafka 4 em KRaft, ELK e Prometheus/Grafana.

## O problema

Um comerciante precisa controlar o fluxo de caixa diário e consultar o saldo consolidado. O serviço de lançamentos não pode parar quando o consolidado cai; em pico, o consolidado recebe 50 requisições por segundo com no máximo 5% de perda. A solução registra o movimento uma única vez, corrige sem apagar o histórico e deriva o saldo sem que a falha de um serviço derrube o outro.

## Arquitetura

- **Lançamentos** grava o lançamento, a chave de idempotência e o evento de outbox na mesma transação, e publica no Kafka.
- **Consolidado** consome o evento, ignora repetidos e mantém a projeção `consolidado_diario` (`saldoDia`, `saldoAnterior`, `saldoAcumulado`).
- O estorno é um **novo lançamento** de tipo oposto; o original nunca é editado.
- O desenho-base está em [`docs/requisitos/Arquietura-solucao-v2.png`](docs/requisitos/Arquietura-solucao-v2.png); o texto com os diagramas C4 e o mapeamento local ↔ AWS está em [`docs/arquitetura/arquitetura-alvo.md`](docs/arquitetura/arquitetura-alvo.md).

Decisões registradas em [ADRs](docs/ultron/agents/adr/): estilo event-driven com outbox (0001), Kafka em KRaft (0002), PostgreSQL com bancos lógicos (0003), idempotência e estorno (0004), projeção e dia do registro (0005), stack .NET 10 (0006) e estrutura em camadas com lib comum (0007). Os requisitos estão no [PRD](docs/requisitos/requisitos.md).

## Como rodar localmente

Pré-requisitos: Docker com Compose (para o MVP), .NET 10 SDK (para os testes) e `jq` (para o roteiro abaixo).

```bash
docker compose up -d --build
observability/scripts/apply-elk.sh   # template de índice, ILM e data view do Kibana
```

> Se a stack já existia de uma execução anterior, rode `docker compose down -v` antes: o init do PostgreSQL cria os usuários por serviço só na primeira subida do volume.


| Serviço | URL |
|---|---|
| Lançamentos (API) | http://localhost:8080 |
| Consolidado (API) | http://localhost:8081 |
| Kibana (logs) | http://localhost:5601 |
| Grafana (métricas) | http://localhost:3000 (admin/admin) |
| Prometheus | http://localhost:9090 |
| Elasticsearch | http://localhost:9200 |

O primeiro `up` baixa as imagens e compila os dois serviços; as migrações rodam na subida.

### Roteiro do demo

```bash
# 1. token de desenvolvimento (Cognito é o alvo; aqui o emissor é local)
TOKEN=$(curl -s -X POST localhost:8080/dev/token -H 'Content-Type: application/json' \
  -d '{"comercianteId":"11111111-1111-1111-1111-111111111111","scopes":["lancamentos.write","consolidado.read"]}' \
  | jq -r .token)

# 2. um crédito de 100
LANCAMENTO=$(curl -s -X POST localhost:8080/api/v1/lancamentos \
  -H "Authorization: Bearer $TOKEN" -H 'Idempotency-Key: demo-1' -H 'Content-Type: application/json' \
  -d '{"valor":100.00,"tipo":"credito","descricao":"venda"}')
echo "$LANCAMENTO"

# 3. o mesmo envio não duplica
curl -s -X POST localhost:8080/api/v1/lancamentos \
  -H "Authorization: Bearer $TOKEN" -H 'Idempotency-Key: demo-1' -H 'Content-Type: application/json' \
  -d '{"valor":100.00,"tipo":"credito","descricao":"venda"}'

# 4. o consolidado do dia (o dia é o do registro, America/Sao_Paulo)
DIA=$(TZ=America/Sao_Paulo date +%F)
curl -s "localhost:8081/api/v1/consolidado/$DIA" -H "Authorization: Bearer $TOKEN"

# 5. estorno do lançamento (contra-lançamento com nova chave)
ID=$(echo "$LANCAMENTO" | jq -r .id)
curl -s -X POST "localhost:8080/api/v1/lancamentos/$ID/estornos" \
  -H "Authorization: Bearer $TOKEN" -H 'Idempotency-Key: demo-estorno-1'

# 6. o consolidado do dia volta a zero
curl -s "localhost:8081/api/v1/consolidado/$DIA" -H "Authorization: Bearer $TOKEN"
```

No Kibana, a data view `cashflow-logs-*` mostra os logs JSON dos dois serviços. No Grafana, o dashboard **Fluxo de caixa — Golden Signals** mostra tráfego, erros, latência, idade do outbox, lag, DLT e lançamentos por resultado. As regras do Prometheus incluem os alarmes desses sinais.

## Testes

```bash
dotnet test --solution Cashflow.slnx
```

A suíte (24 testes — 17 de comportamento e 7 de arquitetura) sobe PostgreSQL e Kafka reais via Testcontainers e cobre: idempotência (replay, 422 de payload, 409 de concorrência), estorno (regras e imutabilidade), o fluxo ponta a ponta até o consolidado, a entrega duplicada somando uma vez, a DLT após 1+3 tentativas, o isolamento (lançamentos de pé com o consolidado fora) a carga de 50 req/s com ≥95% de sucesso e, nos ArchTests, a direção das dependências entre as camadas.

## Observabilidade

A convenção de logs e as métricas estão em [`docs/ultron/agents/logging.md`](docs/ultron/agents/logging.md); o stack local e o mapeamento para o alvo estão em [`observability/README.md`](observability/README.md). A aplicação emite OpenTelemetry e JSON — trocar ELK/Prometheus por CloudWatch é configuração, não código. Os SLOs propostos estão em [`observability/slos/cashflow.yml`](observability/slos/cashflow.yml).

## Diferenciais do desafio

- **Arquitetura-alvo e C4:** [`docs/arquitetura/arquitetura-alvo.md`](docs/arquitetura/arquitetura-alvo.md)
- **Arquitetura de transição:** [`docs/arquitetura/transicao.md`](docs/arquitetura/transicao.md)
- **Estimativa de custos:** [`docs/arquitetura/custos.md`](docs/arquitetura/custos.md)
- **Observabilidade:** [`docs/arquitetura/observabilidade.md`](docs/arquitetura/observabilidade.md)
- **Segurança para consumo dos serviços:** [`docs/arquitetura/seguranca-integracao.md`](docs/arquitetura/seguranca-integracao.md)

## Estrutura

```
src/Cashflow.Common/                      lib comum: auth, Serilog, OpenTelemetry, request context, tempo
src/Cashflow.Contracts/                   envelope do evento, tipos e tópicos (schema do barramento)
src/Cashflow.Lancamentos.Domain/          regras do lançamento e do estorno, dia do negócio, portas
src/Cashflow.Lancamentos.Application/     casos de uso e orquestração da idempotência
src/Cashflow.Lancamentos.Infrastructure/  Dapper, outbox, publicador Kafka, métricas, migrator
src/Lancamentos.Api/                      composição e endpoints do serviço de Lançamentos
src/Cashflow.Consolidado.Domain/          projeção (saldo do dia e acumulado), regra do delta
src/Cashflow.Consolidado.Application/     consulta e aplicação do evento
src/Cashflow.Consolidado.Infrastructure/  Dapper, consumidor Kafka, métricas, migrator
src/Consolidado.Api/                      composição e endpoints do serviço de Consolidado
tests/Lancamentos.Api.Tests/              integração de comportamento (PostgreSQL + Kafka)
tests/Cashflow.ArchTests/                 direção das dependências (NetArchTest)
observability/                            ELK, Prometheus, Grafana, SLOs
infra/postgres/                           criação do banco lógico do consolidado
docs/requisitos/                          PRD e desenho-base
docs/ultron/agents/                       visão, negócio, ADRs, logging, research
compose.yaml                              PostgreSQL, Kafka, serviços, ELK, Prometheus, Grafana
```

## Fora de escopo

Interface gráfica; provisionamento AWS (o alvo é desenho); ERP/contabilidade completa; consolidação em tempo real; migração de legado em código (a transição é documentada); lançamento retroativo (o dia é o do registro); multi-região.

## Notas

- A autenticação local é um **emissor de desenvolvimento** (`/dev/token`, só em Development); o alvo usa Cognito com authorization code + PKCE e client credentials, e os segredos ficam no Secrets Manager.
- O MVP usa uma instância PostgreSQL com dois bancos lógicos; a separação física é a evolução registrada no ADR-0003.
- Entrega do desafio técnico para a vaga de Arquiteto de Soluções do Carrefour, via FCamara.
