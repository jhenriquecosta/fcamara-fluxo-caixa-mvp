# Cashflow.Testing

> A infraestrutura compartilhada dos testes: containers, fábricas dos hosts, tokens e relógio fixo.

## Por que existe

Três projetos de teste sobem os mesmos hosts, contra os mesmos containers, com os mesmos tokens. Se cada um montasse isso por conta própria, as três montagens divergiriam, e um teste poderia passar por estar configurado diferente da produção. Aqui a montagem é uma só.

## O que tem

| Tipo | Papel |
|---|---|
| `PostgresFixture` | PostgreSQL 17 em container, com o esquema dos dois módulos aplicado como o Migrator faria |
| `KafkaFixture` | Kafka 4 (KRaft) em container |
| `WebApiFactory` | o `Cashflow.WebAPI` real, com os módulos que o teste liga (`SoLancamentos`, `SoConsolidado`) e o relógio opcionalmente fixo |
| `BackgroundServerFactory` | o `Cashflow.BackgroundServer` real, com o outbox e/ou o consumidor |
| `TestTokens` | JWT assinado com a chave dos testes, validado de verdade pelo host |
| `ClienteHttp` | `Autenticado(...)` e `EsperarConsolidadoAsync(...)` (espera a projeção eventual) |
| `FixedTimeProvider` | o relógio parado no instante que o teste escolhe |
| `HostSettings` | a configuração entra por variável de ambiente, porque o `Program` a lê antes do `Build` |

## Princípio

Os testes sobem o **mesmo `Program`** que vai para produção. Nada de host alternativo para teste: a única diferença é a configuração.
