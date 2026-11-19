# Cashflow.Modules.Consolidado

> O saldo diário: projeta cada lançamento publicado no saldo do dia do comerciante e responde a consulta do consolidado.

## Por que existe

O comerciante quer ver o saldo do dia, e o requisito pede 50 consultas por segundo sem afetar quem está lançando. Calcular a soma a cada consulta, no banco dos lançamentos, colocaria leitura pesada no caminho da escrita. Este módulo mantém uma **projeção** pronta para ler, alimentada pelos eventos, no seu próprio banco. Se ele cair, os lançamentos continuam; quando voltar, alcança pelo tópico.

## Finalidade

| Caso de uso | Origem | Regras |
|---|---|---|
| `ProjetarMovimentoCommand` | evento do Kafka, via `LancamentoEventConsumer` | crédito soma, débito subtrai; o mesmo `eventId` soma uma vez só |
| `ConsultarConsolidadoDoDiaQuery` | `GET /api/v1/consolidado/{yyyy-MM-dd}` | dia sem movimento responde saldo zero com o acumulado do dia anterior, nunca 404 |

A resposta traz `saldoDia`, `saldoAnterior` (de abertura) e `saldoAcumulado`.

### Consistência eventual

O consolidado fica alguns instantes atrás dos lançamentos, o tempo de o outbox publicar e o consumidor projetar. É a troca consciente pelo isolamento de falha. A métrica `cashflow_consumer_lag` mostra quanto falta.

## Estrutura

| Pasta | Papel |
|---|---|
| `Entities/` | `ConsolidadoDiario` (a leitura do dia) e `MovimentoDoDia` (o lançamento visto pelo consolidado) |
| `Commands/` | a projeção, idempotente e com `[DatabaseRetry]` |
| `Queries/` | a consulta do dia |
| `MessageBusConsumers/` | do evento do barramento ao comando |
| `Repositories/` | a deduplicação (`evento_processado`) e o upsert do saldo, em SQL; o `Schema.sql` embutido |
| `Endpoints/`, `Models/`, `ConfigurationOptions/` | a rota, a resposta e a seção `Modules:Consolidado` |

## Estilo arquitetural e padrões

- **Módulo auto-contido** do Modular Monolith.
- **CQRS com modelo de leitura** (Read Model / Materialized View) alimentado por eventos.
- **Idempotent Consumer**: `INSERT ... ON CONFLICT DO NOTHING` em `evento_processado` na mesma transação da soma.
- **Eventual Consistency** entre os módulos.
- `ConsolidadoDiario` **não é agregado**: é projeção, sem invariante própria além do saldo.

## Dependências

| Pode referenciar | Não pode |
|---|---|
| Application, Domain, Contracts, Infrastructure, CrossCuttingConcerns | `Cashflow.Modules.Lancamentos`, hosts, Confluent.Kafka |

Ele só conhece o lançamento pelo `LancamentoEventPayload` do Contracts. Provado em `tests/Cashflow.ArchTests`.

## Testes

- `tests/Cashflow.Modules.Consolidado.UnitTests`: o efeito no saldo e a consulta de dia sem movimento.
- `tests/Cashflow.Modules.Consolidado.IntegrationTests`: a rota com PostgreSQL real e a carga de 50 req/s.

## Decisões

- [ADR-0005](../../docs/ultron/agents/adr/0005-projecao-consolidado.md): a projeção.
- [ADR-0003](../../docs/ultron/agents/adr/0003-postgresql-bancos-logicos.md): o banco lógico próprio.
