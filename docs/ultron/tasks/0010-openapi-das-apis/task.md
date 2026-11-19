# Task 0010-openapi-das-apis — OpenAPI das APIs de Lançamentos e Consolidado

tipo: feature
branch: feature/0010-openapi-das-apis
publicacao: https://github.com/jhenriquecosta/fcamara-fluxo-caixa-mvp/pull/36
estado: concluído
worktree: .claude/worktrees/feature-0010-openapi-das-apis
responsavel: jhenriquecosta@hotmail.com
next_skill: 
issue: #26

## Contexto

Nenhuma API publica OpenAPI hoje. A referência (ClassifiedAds.WebAPI do ModularMonolith) expõe Swagger com esquema Bearer, e a divergência não está declarada no ADR-0009.

## Escopo

- `Microsoft.AspNetCore.OpenApi` nativo do .NET 10 (`AddOpenApi`/`MapOpenApi`) no `Cashflow.WebAPI`; troca do Swashbuckle da referência registrada no ADR-0009.
- Esquema `Bearer` com os escopos por rota, `ProblemDetails` de erro e o header `Idempotency-Key`.
- Um documento por deploy: só os endpoints do módulo ligado (`Modules:<X>:Enabled`).
- UI interativa (Scalar ou Swagger UI) só em Development.

## Critérios de aceitação

- [x] `/openapi/v1.json` responde em lancamentos-api e consolidado-api, cada um só com as rotas do seu módulo — `OpenApiTests` nos dois projetos (AC1/AC2)
- [x] O documento declara Bearer, os escopos, `Idempotency-Key` e os `ProblemDetails` — esquema OAuth2 client credentials com os escopos por rota (ADR-0013), chave nos dois POSTs e ProblemDetails 401/403 + códigos de contrato; `OpenApiTests` AC3/AC4
- [x] Teste de integração cobre o documento — `OpenApiTests` pelo `WebApiFactory`, afirmando o JSON do documento

## Adiamento (2026-11-19)

Aberta e adiada por decisão do Operador: "abre a feature, mas não vamos desenvolver agora".

**Gatilhos de retomada:** a task 0009 (`refactor/0009-codigo-que-se-explica`) e a 0008 (frontend) entraram na `develop`. Nascer depois da 0009 evita retrabalho com a regra de XML doc (`CS1591` como erro) e com a convenção de nomes de teste. Para retomar, faça o merge da `develop` nesta branch e siga com `/ultron-discover`.

## Fatias publicadas

| # | Fatia | Issue | Estado |
|---|---|---|---|
| 1 | O documento OpenAPI no ar | #33 | concluída |
| 2 | O contrato completo: escopos, chave e erros | #34 | concluída |
| 3 | Scalar em Development | #35 | concluída |

O corpo de cada fatia está em `slices/<issue>.md` (o portão de aceite lê o arquivo antes da issue).

## Fases

- [x] discover — `descoberta.md`
- [x] research — `research.md` (Research #0004)
- [x] prd — `prd.md` (PRD-0005) · ADR-0013
- [x] slices — issues #33–#35
- [x] coder — S1–S3 (commit do documento OpenAPI das APIs)
- [x] verify — 119/119 em 2026-11-19

## Exceções registradas

- **verify pulado no fechamento (`--no-verify`), 2026-11-19.** O `verify_all.sh` roda `dotnet test --no-build -q`; com xunit.v3/Microsoft.Testing.Platform o `-q` é recusado ("Opção desconhecida '--q'", exit 5) — vermelho falso da ferramenta, não do código. Suíte exercida manualmente: `dotnet test --no-build` → 119/119 em 2026-11-19. Mesmo bug registrado nas tasks 0011 e 0012; conserto pendente no `ultron-skills`.
