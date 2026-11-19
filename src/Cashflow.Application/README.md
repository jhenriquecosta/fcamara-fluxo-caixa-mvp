# Cashflow.Application

> O mecanismo dos casos de uso: comandos, consultas, o `Dispatcher` que os executa e os decorators que os envolvem.

## Por que existe

Todo caso de uso do sistema (registrar, estornar, consultar o consolidado, projetar um movimento, publicar o outbox) passa pelo mesmo caminho. Sem este projeto, cada endpoint chamaria o seu serviço de um jeito, e a auditoria e as novas tentativas no banco seriam repetidas à mão em cada lugar. Aqui o caminho é um só: monta-se o comando, entrega-se ao `Dispatcher`, e os decorators pedidos pelo handler são aplicados.

## Finalidade

- **`Common/`**: `ICommand`/`ICommandHandler`, `IQuery`/`IQueryHandler`, o `Dispatcher` (que também despacha os eventos de domínio) e a `HandlerFactory`, que monta o pipeline de decorators.
- **`Decorators/`**: `[AuditLog]` (registra o caso de uso e a duração, sem dados pessoais) e `[DatabaseRetry]` (repete em falha transitória do banco, pelo `DbException.IsTransient` do ADO.NET).
- **`ApplicationServicesExtensions`**: `AddApplicationServices` (o Dispatcher) e `AddMessageHandlers(assembly)`, que cada módulo chama para registrar os próprios handlers.

Não tem caso de uso concreto. Esses moram nos módulos.

## Estilo arquitetural e padrões

- **CQRS** (separação comando/consulta) em processo, sem barramento de mediação externo.
- **Command/Query Handler** com o handler no mesmo arquivo do comando, como na referência.
- **Decorator**, declarado por atributo no handler: o pipeline fica perto da regra, não no host.
- **Mediator** (`Dispatcher`): quem aciona não conhece o handler concreto. O ArchTests proíbe endpoint, worker ou consumidor de depender de handler.

## Dependências

| Pode referenciar | Não pode |
|---|---|
| `Cashflow.Domain`, `Cashflow.CrossCuttingConcerns`, abstrações de DI e de logging | Infrastructure, Contracts, módulos, hosts, Npgsql, Confluent.Kafka |

Provado em `tests/Cashflow.ArchTests` (`Application_nao_conhece_infraestrutura_contratos_nem_modulos`).

## Decisões

- [ADR-0009](../../docs/ultron/agents/adr/0009-monolito-modular.md): o mecanismo da referência adotado de verdade, não só os nomes das pastas.
