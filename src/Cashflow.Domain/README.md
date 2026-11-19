# Cashflow.Domain

> As abstrações do domínio que todos os módulos compartilham: entidade, agregado, evento de domínio, repositório, unidade de trabalho e a porta do barramento.

## Por que existe

Os módulos de Lançamentos e de Consolidado não podem se conhecer (o ArchTests proíbe), mas precisam falar a mesma língua: o que é uma entidade, como se abre uma transação, como se publica uma mensagem. Esta é essa língua. Sem ela, cada módulo definiria seu próprio `IRepository` e seu próprio barramento, e o host não conseguiria compô-los.

## Finalidade

- **`Entities/`**: `Entity<TKey>` (identidade e instante de criação), `IAggregateRoot` (a fronteira de consistência) e `ITrackable`. Não existe `UpdatedDateTime`: no livro-caixa nada é alterado depois de gravado.
- **`Events/`**: `IDomainEvent`, `IDomainEventHandler<T>` e `EntityCreatedEvent<T>`, despachados em processo, na transação do comando.
- **`Repositories/`**: `IRepository<TEntity,TKey>` sem `IQueryable` (com Dapper a consulta é SQL explícito) e `IUnitOfWork` (abrir e confirmar; sair sem confirmar desfaz).
- **`Infrastructure/MessageBrokers/`**: as portas do barramento (`IMessageBus`, `IMessageSender`, `IMessageReceiver`, `IMessageBusConsumer`, `IOutBoxEventPublisher`) e o `MessageBus`, que roteia sem conhecer Kafka.

O domínio de negócio de cada módulo (o `Lancamento`, o `ConsolidadoDiario`) não mora aqui, e sim no módulo.

## Estilo arquitetural e padrões

- **Clean Architecture**: o núcleo, que só aponta para dentro.
- **DDD tático**: Entity, Aggregate Root, Domain Event, Repository, Unit of Work.
- **Ports and Adapters**: as interfaces de mensageria são portas; o Kafka é o adaptador, na Infrastructure.
- **Transactional Outbox**: o `IOutBoxEventPublisher` é a ponta de saída do outbox.

## Dependências

| Pode referenciar | Não pode |
|---|---|
| `Cashflow.CrossCuttingConcerns`, `Microsoft.Extensions.DependencyInjection.Abstractions` | Application, Contracts, Infrastructure, módulos, hosts |

Provado em `tests/Cashflow.ArchTests` (`Domain_conhece_no_maximo_o_CrossCuttingConcerns`).

## Decisões

- [ADR-0001](../../docs/ultron/agents/adr/0001-dois-servicos-outbox-projecao.md): outbox e projeção.
- [ADR-0006](../../docs/ultron/agents/adr/0006-stack-dotnet-10.md): Dapper em vez de EF Core, por isso o repositório sem `IQueryable`.
- [ADR-0009](../../docs/ultron/agents/adr/0009-monolito-modular.md): a estrutura modular.
