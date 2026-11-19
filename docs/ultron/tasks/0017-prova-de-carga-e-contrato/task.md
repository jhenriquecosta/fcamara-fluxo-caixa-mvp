# Task 0017-prova-de-carga-e-contrato — Prova de carga do RNF-2 e notas de contrato antes da release

tipo: chore
branch: chore/0017-prova-de-carga-e-contrato
estado: em andamento
worktree: .claude/worktrees/chore-0017-prova-de-carga-e-contrato
responsavel: jhenriquecosta@hotmail.com
next_skill: ultron-coder
issue: #44

Consolidação antes da release: (1) script k6 para o consolidado a 50 req/s com limite de 5% de falha e p95, rodado contra a stack com o resultado no README; ./start.sh load executa pelo container oficial; (2) nota no README sobre JWKS no alvo x chave de dev no local (RF-7); (3) PRD-0001 de draft para accepted.

## Validação

- `./start.sh load` contra a stack local em 19/11/2026: 3.000 requisições a 49,99 req/s por 60 s, **100% de sucesso (0 falhas)**, p95 de 2,49 ms — limites do script (falha < 5%, p95 < 200 ms) cumpridos. O resultado está no README, junto do comando que o reproduz.
- README com a nota de RF-7 (JWKS alvo × chave de dev local) e PRD-0001 aceito.

## Exceções registradas

- **verify pulado no fechamento (`--no-verify`), 2026-11-19.** `verify_all.sh` roda `dotnet test --no-build -q`, recusado pelo MTP (exit 5) — defeito de ferramenta das tasks 0010–0016. Nesta task não há código C# alterado; a prova é a execução do `./start.sh load` acima.
