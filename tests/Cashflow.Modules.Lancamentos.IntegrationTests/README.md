# Cashflow.Modules.Lancamentos.IntegrationTests

> O módulo de Lançamentos pela API real, contra um PostgreSQL real em container.

## Por que existe

As garantias mais importantes do módulo não estão num tipo, e sim na combinação de HTTP, SQL e transação: a mesma chave não grava duas vezes, o lançamento e o evento do outbox ficam juntos, o segundo estorno é barrado pelo banco. Só um banco de verdade prova isso. O host sobe em memória (`WebApplicationFactory`), com o mesmo `Program` que vai para produção.

## O que cobre

| Classe | O que prova |
|---|---|
| `LancamentoTests` | 201 no dia de São Paulo; lançamento e outbox gravados juntos; 422 para valor ou tipo inválido; 400 sem `Idempotency-Key`; repetição devolve o mesmo lançamento; chave reusada com outro corpo é 422; chave em processamento é 409; métrica exposta |
| `EstornoTests` | contra-lançamento de tipo oposto; original intacto no banco; segundo estorno 409; estorno de estorno 422; lançamento de outro comerciante 404 |
| `AuthTests` | sem token 401; token sem escopo 403 |
| `ModulosTests` | a `lancamentos-api` não expõe o consolidado e não roda trabalho de fundo |
| `HealthTests` | `/health` sem token |

## Como roda

Um PostgreSQL 17 por assembly (`PostgresFixture`, Testcontainers), com o esquema aplicado como o Migrator faria. Precisa de Docker. O relógio do servidor é fixado em 02h30 UTC de 18/11 (23h30 de 17/11 em São Paulo) para provar o dia de negócio.

## Convenção

Nome é a regra, Arrange/Act/Assert, critério em `[Trait("Criterio", "ACn")]`.
