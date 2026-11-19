# Task 0013-guarda-do-documento-openapi — Guarda do contrato OpenAPI: operação aberta não declara 401/403

tipo: chore
branch: chore/0013-guarda-do-documento-openapi
publicacao: https://github.com/jhenriquecosta/fcamara-fluxo-caixa-mvp/pull/38
estado: concluído
worktree: .claude/worktrees/chore-0013-guarda-do-documento-openapi
responsavel: jhenriquecosta@hotmail.com
next_skill: 
issue: #37

O teste da invariante do AC4 da task 0010 ficou fora do PR #36 por ter sido escrito depois do commit de implementação. Esta task traz o teste à develop — operação sem requisito de segurança não declara 401 nem 403 no documento — e recolhe o worktree/branch da 0010.

## Exceções registradas

- **verify pulado no fechamento (`--no-verify`), 2026-11-19.** O `verify_all.sh` roda `dotnet test --no-build -q`; com xunit.v3/Microsoft.Testing.Platform o `-q` é recusado (exit 5) — vermelho falso da ferramenta, não do código. Suíte exercida manualmente: `dotnet test --no-build` → 141/141 em 2026-11-19. Mesmo bug registrado nas tasks 0010, 0011 e 0012; conserto pendente no `ultron-skills`.
