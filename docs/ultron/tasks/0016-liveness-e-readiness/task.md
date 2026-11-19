# Task 0016-liveness-e-readiness — Liveness e readiness separados nos serviços

tipo: chore
branch: chore/0016-liveness-e-readiness
estado: em andamento
worktree: .claude/worktrees/chore-0016-liveness-e-readiness
responsavel: jhenriquecosta@hotmail.com
next_skill: ultron-coder
issue: #43

RF-8 pede health para o balanceador separando liveness e readiness; hoje só existe /health. O ajuste: /health/live (processo vivo, 200 sempre) e /health/ready (dependências — banco dos módulos ligados — 200 ou 503 com o problema), mantendo /health por compatibilidade com compose e start.sh.

## Validação

- `/health/live` 200 em WebAPI e front; `/health/ready` 200 com o banco no ar e **503 com os problemas** quando o banco não responde (teste com connection string inválida). Testes: `HealthTests` (API) e `SaudeTests` (front). Suíte: 147/147.

## Exceções registradas

- **verify pulado no fechamento (`--no-verify`), 2026-11-19.** `verify_all.sh` roda `dotnet test --no-build -q`, recusado pelo MTP (exit 5) — defeito de ferramenta das tasks 0010–0015. Suíte exercida manualmente: `dotnet test --no-build` → 147/147; `dotnet format --verify-no-changes` limpo.
