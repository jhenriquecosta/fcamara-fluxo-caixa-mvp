# Task 0006-versao-1-2-0 — Declara a versão 1.2.0 para a release

tipo: chore
branch: chore/0006-versao-1-2-0
estado: em andamento
responsavel: jhenriquecosta@hotmail.com
next_skill: ultron-coder
issue: #19

Os commits desde v1.1.0 são refactor/chore, e o versionize deriva bump nenhum (proposta 1.1.0, tag já existente). A versão 1.2.0 é decisão humana, declarada em VERSION: a release leva a estrutura modular (ADR-0009), o CI no GitHub Actions e a correção observável de "tipo":"1" (antes crédito, agora 422). Precedente: 1.0.0 → 1.1.0 também foi reestruturação.

## Critérios de aceitação

- [x] `VERSION` declara `1.2.0`, e `versionize.py propose` passa a propor 1.2.0 (declaração à frente da tag).

## Exceções registradas

- **verify do fechamento pulado (`--no-verify`):** mesmo motivo da task 0005 (ultron-skills#481). A mudança é só o arquivo `VERSION`; o CI do PR é a verificação.
