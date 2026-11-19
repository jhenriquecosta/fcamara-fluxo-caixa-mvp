# Task 0016-liveness-e-readiness — Liveness e readiness separados nos serviços

tipo: chore
branch: chore/0016-liveness-e-readiness
estado: em andamento
worktree: .claude/worktrees/chore-0016-liveness-e-readiness
responsavel: jhenriquecosta@hotmail.com
next_skill: ultron-coder
issue: #43

RF-8 pede health para o balanceador separando liveness e readiness; hoje só existe /health. O ajuste: /health/live (processo vivo, 200 sempre) e /health/ready (dependências — banco dos módulos ligados — 200 ou 503 com o problema), mantendo /health por compatibilidade com compose e start.sh.
