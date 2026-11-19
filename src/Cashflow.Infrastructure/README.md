# Cashflow.Infrastructure

> Os adaptadores para o mundo de fora: Kafka, PostgreSQL, Serilog, OpenTelemetry, JWT, e o pipeline HTTP comum a todos os hosts.

## Por que existe

O Domain declara portas (`IMessageBus`, `IUnitOfWork`, `IDateTimeProvider`) e não sabe que existe Kafka nem PostgreSQL. Alguém precisa implementar essas portas, e precisa ser um lugar só: dois hosts configurando log, autenticação e métricas cada um do seu jeito divergiriam na primeira mudança. A Infrastructure é esse lugar, e todo host a liga com `AddCashflowInfrastructure` e `UseCashflowWeb`.

## Finalidade

| Pasta | O que faz |
|---|---|
| `MessageBrokers/Kafka` | `KafkaSender` (acks=all, producer idempotente, chave de partição) e `KafkaReceiver` (commit manual, 4 tentativas, DLT) |
| `MessageBrokers` | o registro do broker no DI (`AddMessageBusSender/Receiver`) e as métricas de lag e DLT |
| `HostedServices` | `MessageBusConsumerBackgroundService`: mantém um consumidor rodando enquanto o host vive |
| `Persistence` | `DbContext` sobre Dapper (conexão e transação por escopo), data source por módulo, aplicação do `Schema.sql` |
| `Security` | JWT Bearer, políticas por escopo, extração do comerciante do token, token de desenvolvimento |
| `Logging` | Serilog em JSON (console, arquivo e, opcionalmente, Elasticsearch) |
| `Monitoring` | OpenTelemetry com exportador Prometheus |
| `Web` | `/health`, `/metrics`, `/dev/token`, correlação de requisição e as respostas de erro (`CashflowProblems`, RFC 9457) |
| `Configuration` | os padrões de configuração, com a menor precedência |
| `DateTimes` | o `IDateTimeProvider` sobre o `TimeProvider` e o fuso do negócio |

## Estilo arquitetural e padrões

- **Clean Architecture**: o anel externo; implementa as portas do Domain.
- **Ports and Adapters**: Kafka e PostgreSQL são adaptadores trocáveis atrás das interfaces.
- **Retry + Dead Letter Queue** no consumo; **at-least-once** com consumidor idempotente.
- **Unit of Work** sobre ADO.NET/Dapper.
- **Problem Details** (RFC 9457) para todos os erros HTTP.

## Dependências

| Pode referenciar | Não pode |
|---|---|
| Domain, Application, Contracts, CrossCuttingConcerns, Confluent.Kafka, Npgsql, Dapper, Serilog, OpenTelemetry, ASP.NET Core | módulos, hosts |

Provado em `tests/Cashflow.ArchTests` (`Infrastructure_nao_conhece_modulos_nem_hosts`). Só ela e os testes referenciam Confluent.Kafka; os módulos falam com o broker só pelo `IMessageBus`.

## Decisões

- [ADR-0002](../../docs/ultron/agents/adr/0002-kafka-kraft-barramento.md): Kafka em KRaft.
- [ADR-0003](../../docs/ultron/agents/adr/0003-postgresql-bancos-logicos.md): um banco lógico por módulo.
- [ADR-0005](../../docs/ultron/agents/adr/0005-projecao-consolidado.md): novas tentativas e DLT no consumo.
- [ADR-0006](../../docs/ultron/agents/adr/0006-stack-dotnet-10.md): .NET 10 e Dapper.
