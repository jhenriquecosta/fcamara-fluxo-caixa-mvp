# Cashflow.Migrator

> Aplica o esquema de cada módulo no banco dele, uma vez, antes de os hosts subirem.

## Por que existe

Se cada réplica da API aplicasse o esquema ao subir, várias réplicas o fariam ao mesmo tempo, e a API precisaria de permissão de DDL no banco. O Migrator roda antes, como um job (`depends_on: service_completed_successfully` no compose), com a permissão que só ele precisa. É o `Migrator` da referência; lá com EF Migrations, aqui com os `Schema.sql` dos módulos (Dapper, ADR-0006).

## Finalidade

Para cada módulo, lê `Modules:<X>:ConnectionStrings:Default` e aplica o `Schema.sql` embutido no assembly do módulo (`MigrateLancamentosDbAsync`, `MigrateConsolidadoDbAsync`). Os scripts são idempotentes (`IF NOT EXISTS`, migrações aditivas): rodar de novo não muda nada. Sai com erro se faltar connection string.

## Estilo arquitetural e padrões

- **Migration as a job**: separado do runtime, executado uma vez por deploy.
- **Schema ownership por módulo**: o esquema mora no módulo; o Migrator só o executa.

## Dependências

Referencia os dois módulos, só para chamar a migração de cada um.

## Decisões

- [ADR-0003](../../docs/ultron/agents/adr/0003-postgresql-bancos-logicos.md) e [ADR-0006](../../docs/ultron/agents/adr/0006-stack-dotnet-10.md).
