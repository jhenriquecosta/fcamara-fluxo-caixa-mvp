# Task 0021-requisitos-obrigatorios-e-coerencia — Requisitos obrigatórios em destaque e coerência pós-0019

tipo: docs
branch: docs/0021-requisitos-obrigatorios-e-coerencia
publicacao: pendente
estado: concluído
worktree: .claude/worktrees/docs-0021-requisitos-obrigatorios-e-coerencia
responsavel: jhenriquecosta@hotmail.com
issue: #52

Origem: avaliação de `.tmp/pendencias-revisao-banca.md` (itens 1 e 6). Parte de código já entregue na task 0020.

## Critérios de aceitação

- [x] O README tem a seção `## Documentação dos requisitos obrigatórios`, antes dos diferenciais, com link para o mapeamento de domínios e capacidades (`docs/ultron/agents/business/`), PRD, arquitetura-alvo + C4 e ADRs — `README.md`, seção nova antes de *Diferenciais do desafio*
- [x] README, `avaliacao-de-padroes.md` e `seguranca-integracao.md` refletem que o Strategy de validação do JWT (OIDC/JWKS por `Auth:Authority`) já está implementado — README (Arquitetura), `avaliacao-de-padroes.md` (status, resumo, seções 2 e 5, ordem), `seguranca-integracao.md`, ADR-0012 item 5
- [x] A contagem de testes do README bate com a suíte medida (151; `Lancamentos.IntegrationTests` 37, `Consolidado.IntegrationTests` 7) — medido projeto a projeto em 23/11: **154** no total (Lançamentos 24 + 37, Consolidado 8 + 7, E2E 5, Blazor 33, Terminal 19, Arch 21); a tabela do README foi atualizada linha a linha
- [x] O texto distingue o ADR-010 do diagrama-base do ADR-0010 da série local — `README.md:3,18` e `arquitetura-alvo.md:3`
- [x] Polimentos: comentário do `MarcarFalhaAsync` no estorno, `/health/ready` sem `ex.Message` cru, `MovimentoDoDia` recusa tipo desconhecido — `Cashflow.Contracts.TiposDeLancamento` usado pelos dois módulos; teste `Tipo_fora_do_contrato_e_recusado_em_vez_de_virar_debito` (3 casos)

## Observação de escopo

A task é `docs`, mas os polimentos do item 6 mexem em código (contrato do tipo, `/health/ready`, comentário do estorno). Por isso rodou a mesma validação de uma task de código: build Release com 0 avisos, `dotnet format` limpo e 154/154 testes.

## Exceção de portão

- **verify (`--no-verify`)**: o `verify_all.sh:280` do arsenal roda `dotnet test --no-build -q`, que o xunit.v3/MTP recusa (exit 5). A suíte real foi medida sem `-q`, projeto a projeto: 154/154 em 23/11/2026. O conserto é no `ultron-skills`.
