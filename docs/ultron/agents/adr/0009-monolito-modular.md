# ADR-0009 — Monólito modular no molde do ModularMonolith de referência

**Status:** accepted
**Data:** 2026-11-18
**Decisores:** J Henrique Costa (Operador) · ULTRON
**Versão do documento:** 1.0
**Substitui:** ADR-0007 (camadas e lib comum)
**Relacionados:** ADR-0001 (dois serviços, outbox), ADR-0002 (Kafka), ADR-0003 (bancos por serviço), ADR-0005 (projeção), ADR-0006 (stack, Dapper)

## Contexto

O ADR-0007 separou cada serviço em camadas (`Domain`/`Application`/`Infrastructure`/`Api`) e criou uma lib comum. O Operador reprovou o resultado duas vezes. A separação era por camada, não por módulo. Na segunda passada, os nomes de pasta do `src/ModularMonolith` do [Practical.CleanArchitecture](https://github.com/jhenriquecosta/Practical.CleanArchitecture/tree/master/src/ModularMonolith) foram copiados sem o mecanismo:

- o `ICommand` existia e nenhum módulo o usava;
- os endpoints injetavam o handler concreto;
- o consumer chamava o Confluent.Kafka direto;
- os workers foram para um `BackgroundServer` que nem o compose nem os testes subiam.

O Operador definiu a referência como template obrigatório.

O requisito de falha isolada (`requisitos.md`: *"o serviço de controle de lançamento não pode ficar indisponível se o consolidado cair"*) parece colidir com a referência, que tem **um** `WebAPI`.

## Decisão

Adotar o ModularMonolith da referência como estrutura e como mecanismo.

**Projetos compartilhados.** Cada um tem o papel e a direção de dependência da referência:

| Projeto | Conteúdo | Depende de |
|---|---|---|
| `Cashflow.CrossCuttingConcerns` | `IDateTimeProvider`, exceções, extensões de tipo, pseudônimos de log (`PseudonimoDeLog`) | nada |
| `Cashflow.Domain` | `Entity<TKey>`, `IAggregateRoot`, `IDomainEvent`/`EntityCreatedEvent<T>`, `IRepository`/`IUnitOfWork`, `IMessageBus`, `IMessageSender`, `IMessageReceiver`, `IMessageBusConsumer`, `IOutBoxEventPublisher`, `MessageBus` | CrossCuttingConcerns |
| `Cashflow.Application` | `ICommand`/`ICommandHandler`, `IQuery`/`IQueryHandler`, `Dispatcher` (comandos, consultas, eventos de domínio), `HandlerFactory`, decorators `[AuditLog]` e `[DatabaseRetry]` | Domain |
| `Cashflow.Contracts` | o envelope `CashflowEvent<T>`, `LancamentoEventPayload`, tipos de evento, tópicos | nada |
| `Cashflow.Infrastructure` | Kafka (`KafkaSender`, `KafkaReceiver` com retentativa e DLT), `MessageBusConsumerBackgroundService`, `DbContext` sobre Dapper, Serilog, OpenTelemetry, auth, `DateTimeProvider` | Domain, Contracts |

**Módulos autocontidos.** `Cashflow.Modules.Lancamentos` e `Cashflow.Modules.Consolidado` usam as pastas da referência: `Commands`, `Queries`, `Entities`, `EventHandlers`, `OutBoxEventPublishers`, `MessageBusConsumers`, `HostedServices`, `Repositories`, `Models`, `Endpoints`, `ConfigurationOptions`, `Constants` e a `<X>ModuleServiceCollectionExtensions`. Cada caso de uso é um `ICommand` ou `IQuery` com o handler no mesmo arquivo. Endpoints, workers e consumers acionam o caso de uso pelo `Dispatcher`.

**O outbox segue o caminho da referência.**

1. O handler grava o lançamento e despacha `EntityCreatedEvent<Lancamento>`.
2. O `LancamentoCreatedEventHandler` grava o `OutboxEvent` na mesma transação.
3. O `PublishEventWorker` despacha o `PublishEventsCommand`.
4. O `PublishEventsCommand` entrega cada evento ao `IMessageBus`.
5. O `LancamentoOutBoxEventPublisher` publica no barramento.
6. Do outro lado, o `KafkaReceiver` chama o `LancamentoEventConsumer`.
7. O `LancamentoEventConsumer` despacha o `ProjetarMovimentoCommand`.

**Hosts.**

- `Cashflow.WebAPI` carrega todos os módulos, como na referência, e liga cada um por `Modules:<X>:Enabled`.
- `Cashflow.BackgroundServer` roda o outbox e o consumer, com os mesmos interruptores.
- `Cashflow.Migrator` aplica o esquema de cada módulo e sai.

**Falha isolada no deploy, não no código.** O compose sobe o mesmo `Cashflow.WebAPI` duas vezes:

- `lancamentos-api`, só com Lançamentos ligado;
- `consolidado-api`, só com Consolidado ligado.

A esses dois somam-se o `background-server` e o `migrator`. Uma queda do consolidado não derruba os lançamentos, porque são processos separados com o mesmo binário. Cada módulo tem seu `NpgsqlDataSource` registrado com a chave do módulo, e no BackgroundServer, que carrega os dois, cada um continua no seu banco (ADR-0003).

### Divergências declaradas da referência

- **EF Core → Dapper** (ADR-0006). O `DbContext` é uma unidade de trabalho sobre Npgsql: uma conexão por escopo e a transação compartilhada pelos repositórios do módulo. O `IRepository` fica sem `IQueryable`. Por isso não há `CrudService` nem os comandos genéricos de entidade: o domínio é um razão imutável, não um CRUD.
- **Controllers → minimal endpoints.** Mesma superfície HTTP, e o papel do controller é o mesmo: validar, montar o comando e despachar.
- **Migrations → scripts SQL** embutidos e idempotentes, com migração aditiva quando o esquema muda.
- **Só Kafka.** O `MessageBrokerOptions` mantém o ponto de troca, mas os outros brokers da referência não foram trazidos.
- **`MetaData` com `EventType` e `PartitionKey`.** O mesmo payload serve a dois eventos (registrado e estornado), e a chave de partição mantém a ordem por comerciante (ADR-0002).
- **Comando com resultado.** O resultado do registro volta numa propriedade do comando (`Resultado`), como o `SentEventsCount` do `PublishEventsCommand` da referência, em vez de um `ICommandHandler<T, TResult>`.
- **Sem marcadores `IMessageBusMessage`.** O `Contracts` fica sem dependência, e por isso o tipo da mensagem não implementa interface do Domain.

## Alternativas consideradas

- **Um `WebAPI` só, um processo.** É 100% a referência, mas quebra o requisito de falha isolada e o ADR-0001. Rejeitada.
- **Dois projetos host (`Lancamentos.Api`, `Consolidado.Api`).** Desvia da referência na camada que o Operador apontou e duplica a composição. Rejeitada.
- **Cada API rodar o próprio worker, como na 1.1.0.** É a correção mais curta, mas deixa o `BackgroundServer` sem uso e mistura HTTP com trabalho de fundo. Rejeitada.

## Consequências

- Os ArchTests provam o mecanismo, não só a direção:
  - todo `ICommand`/`IQuery` tem handler e mora em `Commands`/`Queries`;
  - nenhuma classe chamada `*Handler` fica fora das interfaces;
  - endpoints, workers, consumers e publishers não dependem de handler concreto;
  - endpoints dependem do `Dispatcher`;
  - os módulos não conhecem o Confluent.Kafka.
- Os testes seguem a referência: `Modules.<X>.UnitTests`, `Modules.<X>.IntegrationTests` e `EndToEndTests`. O ponta a ponta sobe a topologia do compose dentro do processo de teste.
- Custo: o deploy tem quatro serviços de aplicação em vez de dois, e o `BackgroundServer` é ponto único dos trabalhos de fundo. Se cair, os lançamentos continuam aceitando e o outbox acumula. Ele escala com mais réplicas porque o outbox usa `SKIP LOCKED` e o consumer usa grupo do Kafka.
- O comportamento HTTP não muda, com uma correção: `"tipo": "1"` era aceito como crédito (`Enum.TryParse`) e agora responde 422, como o contrato sempre disse.

## Histórico

| Versão | Data | Autor | Mudança |
|---|---|---|---|
| 1.0 | 2026-11-18 | ULTRON | Monólito modular no molde da referência; WebAPI implantado duas vezes; substitui o ADR-0007 |
