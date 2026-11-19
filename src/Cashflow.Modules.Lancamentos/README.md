# Cashflow.Modules.Lancamentos

> O livro-caixa: registra créditos e débitos do comerciante, corrige com estorno e publica cada fato para quem precisar saber.

## Por que existe

É o requisito central do desafio: o comerciante registra o que entra e o que sai do caixa, e esse registro não pode parar nem duplicar. Este módulo é o dono do lançamento. Ele não sabe que o consolidado existe: publica o fato (`lancamento.registrado`, `lancamento.estornado`), e quem quiser escuta. É por isso que a queda do consolidado não afeta os lançamentos.

## Finalidade

| Caso de uso | Rota | Regras |
|---|---|---|
| `RegistrarLancamentoCommand` | `POST /api/v1/lancamentos` | valor > 0; tipo `debito` ou `credito`; dia de negócio em São Paulo; `Idempotency-Key` obrigatório |
| `EstornarLancamentoCommand` | `POST /api/v1/lancamentos/{id}/estornos` | contra-lançamento de tipo oposto; só lançamento do próprio comerciante; estorno de estorno é 422; um estorno por lançamento (409) |
| `PublishEventsCommand` | (sem rota; `PublishEventWorker` no BackgroundServer) | publica o outbox em lotes de 100, at-least-once |

O lançamento é **imutável**: não há edição nem exclusão. A correção é um estorno, e o original fica na história.

### O caminho de um lançamento

1. O endpoint valida o HTTP e despacha o comando pelo `Dispatcher`.
2. O handler reserva a chave de idempotência (fora da transação, para a requisição concorrente enxergar).
3. Numa transação: grava o `Lancamento`; o `EntityCreatedEvent` faz o `LancamentoCreatedEventHandler` gravar o evento no `outbox_event`; a chave é concluída com a resposta.
4. O `PublishEventWorker`, no BackgroundServer, lê o outbox e o `LancamentoOutBoxEventPublisher` publica no Kafka com o comerciante como chave de partição.

## Estrutura

| Pasta | Papel |
|---|---|
| `Entities/` | `Lancamento` (raiz do agregado), `TipoLancamento`, `DiaDeNegocio`, `OutboxEvent` |
| `Commands/` | os casos de uso de escrita e a regra de idempotência comum (`ChaveDeIdempotencia`) |
| `EventHandlers/` | do evento de domínio ao outbox |
| `OutBoxEventPublishers/` | do outbox ao barramento |
| `HostedServices/` | o worker do outbox |
| `Repositories/` | SQL explícito (Dapper) e o `Schema.sql` embutido |
| `Endpoints/` | minimal API: valida, despacha, traduz `ResultadoDoLancamento` em status |
| `Models/` | o que entra e sai da API |
| `Observability/` | métricas de negócio (lançamentos por desfecho, idade do outbox) |
| `ConfigurationOptions/`, `Constants/` | a seção `Modules:Lancamentos` e os tipos de evento |

## Estilo arquitetural e padrões

- **Módulo auto-contido** do Modular Monolith: entidades, casos de uso, persistência e rotas juntos; o host só liga.
- **DDD tático**: `Lancamento` é a raiz do agregado, com as regras no próprio tipo (fábricas `Registrar`/`Estornar`).
- **CQRS** pelo `Dispatcher`; **Decorator** `[AuditLog]`.
- **Transactional Outbox**: o lançamento e o evento numa transação, a publicação depois.
- **Idempotent Receiver** por `Idempotency-Key` por comerciante.
- **Livro-razão imutável com estorno** (contra-lançamento) em vez de update/delete.

## Dependências

| Pode referenciar | Não pode |
|---|---|
| Application, Domain, Contracts, Infrastructure, CrossCuttingConcerns | `Cashflow.Modules.Consolidado`, hosts, Confluent.Kafka |

Provado em `tests/Cashflow.ArchTests` (`Camadas` e `Modulos`).

## Testes

- `tests/Cashflow.Modules.Lancamentos.UnitTests`: as regras do agregado, sem infraestrutura.
- `tests/Cashflow.Modules.Lancamentos.IntegrationTests`: o módulo pela API, com PostgreSQL real.

## Decisões

- [ADR-0001](../../docs/ultron/agents/adr/0001-dois-servicos-outbox-projecao.md): outbox e isolamento de falha.
- [ADR-0004](../../docs/ultron/agents/adr/0004-idempotencia-e-estorno.md): idempotência e estorno.
- [ADR-0008](../../docs/ultron/agents/adr/0008-event-sourcing-vs-ledger.md): livro-razão em vez de event sourcing.
- [ADR-0009](../../docs/ultron/agents/adr/0009-monolito-modular.md): o módulo e a correção do `"tipo":"1"`.
