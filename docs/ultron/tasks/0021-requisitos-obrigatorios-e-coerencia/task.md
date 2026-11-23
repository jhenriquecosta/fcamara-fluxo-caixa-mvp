# Task 0021-requisitos-obrigatorios-e-coerencia — Requisitos obrigatórios em destaque e coerência pós-0019

tipo: docs
branch: docs/0021-requisitos-obrigatorios-e-coerencia
estado: em andamento
worktree: .claude/worktrees/docs-0021-requisitos-obrigatorios-e-coerencia
responsavel: jhenriquecosta@hotmail.com
next_skill: ultron-coder
issue: #52

Origem: avaliação de `.tmp/pendencias-revisao-banca.md` (itens 1 e 6). Parte de código já entregue na task 0020.

## Critérios de aceitação

- [ ] O README tem a seção `## Documentação dos requisitos obrigatórios`, antes dos diferenciais, com link para o mapeamento de domínios e capacidades (`docs/ultron/agents/business/`), PRD, arquitetura-alvo + C4 e ADRs
- [ ] README, `avaliacao-de-padroes.md` e `seguranca-integracao.md` refletem que o Strategy de validação do JWT (OIDC/JWKS por `Auth:Authority`) já está implementado
- [ ] A contagem de testes do README bate com a suíte medida (151; `Lancamentos.IntegrationTests` 37, `Consolidado.IntegrationTests` 7)
- [ ] O texto distingue o ADR-010 do diagrama-base do ADR-0010 da série local
- [ ] Polimentos: comentário do `MarcarFalhaAsync` no estorno, `/health/ready` sem `ex.Message` cru, `MovimentoDoDia` recusa tipo desconhecido
