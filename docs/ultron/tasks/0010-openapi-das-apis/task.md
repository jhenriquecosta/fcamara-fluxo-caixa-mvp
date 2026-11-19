# Task 0010-openapi-das-apis — OpenAPI das APIs de Lançamentos e Consolidado

tipo: feature
branch: feature/0010-openapi-das-apis
estado: em andamento
worktree: .claude/worktrees/feature-0010-openapi-das-apis
responsavel: jhenriquecosta@hotmail.com
next_skill: ultron-research
issue: #26

## Contexto

Nenhuma API publica OpenAPI hoje. A referência (ClassifiedAds.WebAPI do ModularMonolith) expõe Swagger com esquema Bearer, e a divergência não está declarada no ADR-0009.

## Escopo

- `Microsoft.AspNetCore.OpenApi` nativo do .NET 10 (`AddOpenApi`/`MapOpenApi`) no `Cashflow.WebAPI`; troca do Swashbuckle da referência registrada no ADR-0009.
- Esquema `Bearer` com os escopos por rota, `ProblemDetails` de erro e o header `Idempotency-Key`.
- Um documento por deploy: só os endpoints do módulo ligado (`Modules:<X>:Enabled`).
- UI interativa (Scalar ou Swagger UI) só em Development.

## Critérios de aceitação

- [ ] `/openapi/v1.json` responde em lancamentos-api e consolidado-api, cada um só com as rotas do seu módulo
- [ ] O documento declara Bearer, os escopos, `Idempotency-Key` e os `ProblemDetails`
- [ ] Teste de integração cobre o documento

## Adiamento (2026-11-19)

Aberta e adiada por decisão do Operador: "abre a feature, mas não vamos desenvolver agora".

**Gatilhos de retomada:** a task 0009 (`refactor/0009-codigo-que-se-explica`) e a 0008 (frontend) entraram na `develop`. Nascer depois da 0009 evita retrabalho com a regra de XML doc (`CS1591` como erro) e com a convenção de nomes de teste. Para retomar, faça o merge da `develop` nesta branch e siga com `/ultron-discover`.
