# Task 0003-boilerplate-comum — Boilerplate comum: base de dados, configuração, JSON, exceções e DI

tipo: refactor
branch: refactor/0003-boilerplate-comum
estado: em andamento
responsavel: jhenriquecosta@hotmail.com
next_skill: ultron-coder
issue: #11

## Contexto

O Operador pediu levar para o `Cashflow.Common` todo e qualquer código repetido, tornando-o boilerplate para base de dados, configuração, JSON, exceções, injeção de dependência e mensageria. A revisão aponta duplicação remanescente: o `NpgsqlDataSource` registrado duas vezes, os dois `Migrator` idênticos, as `JsonSerializerOptions` Web em quatro lugares, o hash do comerciante repetido, os `JsonDocument` com strings mágicas no publicador/consumidor, os `Problem` dos endpoints, a leitura do Kafka repetida e os appsettings com o mesmo bloco compartilhado.

## Escopo

- **Base de dados:** `AddCashflowPostgres` (o `NpgsqlDataSource` + a validação da connection string) e `SqlSchema` (o migrator genérico por recurso embutido) no Common; os dois `Migrator` e as duas registrações removidos.
- **Configuração:** os defaults compartilhados (Auth, fuso, níveis do Serilog, tópicos do Kafka) numa fonte do Common, com os appsettings só com o que é do serviço; o `KafkaOptions` tipado.
- **JSON:** o `CashflowJson` (as opções Web) e o `CashflowEventSerializer` tipado no Contracts; os `JsonDocument` e as opções duplicadas removidos.
- **Exceções:** a `CashflowConfigurationException` e os ProblemDetails padrão (`CashflowProblems`) no Common.
- **DI e mensageria:** as extensões do Common (Postgres, Kafka) e os builders de produtor/consumidor/DLT.
- **Hashers** (comerciante/chave) no Common.

## Critérios de aceitação

- [x] Build e a suíte (24) verdes.
- [x] O `docker compose up` sobe e o demo do README passa.
- [x] Nenhum comportamento observável muda.
- [x] ArchTest atualizado: Application/Infrastructure podem referenciar o Common; o Domain não; o Contracts não referencia serviço nem o Common.
