# Task 0004-modular-monolith — Estrutura modular: Cashflow.Modules.* e a infraestrutura de referência

tipo: refactor
branch: refactor/0004-modular-monolith
estado: em andamento
responsavel: jhenriquecosta@hotmail.com
next_skill: ultron-coder
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

## Critérios de aceitação

- [ ] A suíte (24+) verde.
- [ ] O `docker compose up` sobe e o demo do README passa.
- [ ] As dependências: os módulos podem referenciar os compartilhados; os compartilhados não referenciam módulo; o Domain no máximo o CrossCuttingConcerns; o Contracts sem dependência.
- [ ] Nenhum comportamento observável muda.
