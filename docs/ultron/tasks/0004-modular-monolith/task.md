# Task 0004-modular-monolith — Estrutura modular: Cashflow.Modules.* e a infraestrutura de referência

tipo: refactor
branch: refactor/0004-modular-monolith
publicacao: https://github.com/jhenriquecosta/fcamara-fluxo-caixa-mvp/pull/16
estado: concluído
responsavel: jhenriquecosta@hotmail.com
next_skill: 
issue: #15

## Contexto

O Operador reprovou a estrutura anterior: há separação por camadas, não separação de módulos. A referência obrigatória é o `src/ModularMonolith` do Practical.CleanArchitecture: compartilhados (CrossCuttingConcerns, Domain, Application, Contracts, Infrastructure) + `Modules.<X>` auto-contidos + hosts (WebAPI/BackgroundServer/Migrator) + testes por módulo (Unit/Integration/EndToEnd) + ArchTests.

## Escopo

- **Cashflow.CrossCuttingConcerns:** as interfaces e utilitários (exceções, extensões, IDateTimeProvider).
- **Cashflow.Domain:** Entity, IAggregateRoot, ITrackable, IDomainEvent, IRepository, IUnitOfWork, Specification, ValueObjects e as portas de mensageria (IMessageBus, IOutBoxEventPublisher).
- **Cashflow.Application:** o Common (ICommand, ICommandHandler, IQuery, IQueryHandler, Dispatcher, HandlerFactory, CrudService) e os Decorators.
- **Cashflow.Contracts:** o schema do evento (CashflowEvent, payload, EventTypes, Topics, CashflowJson, CashflowEventSerializer).
- **Cashflow.Infrastructure:** as implementações — Serilog, OpenTelemetry, Kafka, Postgres, configuração, tempo, health e auth.
- **Cashflow.Modules.Lancamentos:** Commands (Registrar/Estornar), Entities (Lancamento, Idempotencia, OutboxEvent), Repositories, OutBoxEventPublishers, PublishEventWorker, ConfigurationOptions, Constants, Endpoints, Models e a LancamentosModuleServiceCollectionExtensions.
- **Cashflow.Modules.Consolidado:** Consultar, EventHandler (LancamentoRegistrado), Entities (ConsolidadoDiario, EventoProcessado), Repositories, HostedServices (consumer), ConfigurationOptions, Constants, Endpoints, Models e a ConsolidadoModuleServiceCollectionExtensions.
- **Hosts:** Cashflow.Lancamentos.Api, Cashflow.Consolidado.Api (Program + ModulesOptions), Cashflow.Migrator (runner DbUp-style com os scripts SQL), Cashflow.BackgroundServer.
- **Testes:** Modules.Lancamentos.UnitTests/IntegrationTests, Modules.Consolidado.UnitTests/IntegrationTests, EndToEndTests, ArchTests.
- Compose/Dockerfiles, README e o ADR da estrutura modular (o ADR-0007 vira superseded).

## Divergências declaradas da referência

- EF Core → Dapper (ADR-0006): o módulo tem Repositories + schema SQL, não DbContext EF.
- Controllers → minimal endpoints: o módulo expõe Endpoints/ com a mesma superfície HTTP.
- O Migrator é runner com scripts SQL, como o DbUp da referência.

## Reavaliação (2026-11-18)

A primeira passada copiou os nomes de pasta da referência sem o mecanismo: `ICommand` existia e nenhum módulo o usava; os endpoints injetavam o handler concreto; o consumer falava com o Confluent.Kafka direto; os workers foram para um `BackgroundServer` que nada subia (as 5 falhas do e2e). O Operador reprovou e pediu a referência como template, de verdade.

**Decisão de hosts (opção 1, aprovada):** um `Cashflow.WebAPI` igual à referência, implantado duas vezes com `Modules:<X>:Enabled` — `lancamentos-api` e `consolidado-api` — mais `Cashflow.BackgroundServer` (outbox + consumer, também com os interruptores) e `Cashflow.Migrator`. A falha isolada do requisito vive no deploy, não em projetos duplicados.

**Fatias:**
1. Domain/Application iguais à referência (ICommand/IQuery + handlers no mesmo arquivo, Dispatcher com eventos de domínio, HandlerFactory, Decorators, MessageBus, IMessageSender/IMessageReceiver/IMessageBusConsumer, IOutBoxEventPublisher).
2. Infrastructure: MessageBrokers (KafkaSender/KafkaReceiver com retry+DLT, MessageBrokerOptions), MessageBusConsumerBackgroundService, Persistence (DbContext Dapper como unidade de trabalho).
3. Módulo Lançamentos: Commands (Registrar, Estornar, PublishEvents), EventHandlers (o outbox nasce do EntityCreatedEvent), OutBoxEventPublishers, Entities, Models, Repositories, Endpoints via Dispatcher.
4. Módulo Consolidado: Queries, Commands (Projetar), MessageBusConsumers, Models, Repositories, Endpoints via Dispatcher.
5. Hosts (WebAPI, BackgroundServer, Migrator), Dockerfiles, compose, Prometheus, README, ADR que substitui o ADR-0007.
6. Testes por módulo (Unit/Integration), EndToEnd, ArchTests que proíbem handler sem interface e endpoint que resolve handler concreto.

## Critérios de aceitação

- [x] A suíte verde: 57/57 (17 unidade, 17 integração, 5 ponta a ponta, 18 arquitetura).
- [x] O `docker compose up` sobe (migrator, lancamentos-api, consolidado-api, background-server) e o demo do README passa; falha isolada conferida parando o consolidado.
- [x] As dependências: os módulos podem referenciar os compartilhados; os compartilhados não referenciam módulo; o Domain no máximo o CrossCuttingConcerns; o Contracts sem dependência.
- [x] Nenhum comportamento observável muda — exceto a correção de `"tipo":"1"` (antes aceito como crédito, agora 422), registrada no ADR-0009.
