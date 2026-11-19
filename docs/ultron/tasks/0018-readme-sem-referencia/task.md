# Task 0018-readme-sem-referencia — README explica o porquê dos padrões e linka os READMEs dos projetos

tipo: chore
branch: chore/0018-readme-sem-referencia
publicacao: https://github.com/jhenriquecosta/fcamara-fluxo-caixa-mvp/pull/48
estado: concluído
worktree: .claude/worktrees/chore-0018-readme-sem-referencia
responsavel: jhenriquecosta@hotmail.com
next_skill: 
issue: #47

Ajuste de entrega: remover as citações ao repositório Practical.CleanArchitecture do README e explicar o porquê dos estilos adotados (monólito modular, command/query com dispatcher, BFF, outbox) por si mesmos; acrescentar uma seção com os links dos READMEs de cada projeto (src, tests, observabilidade).

## Validação

- Nenhuma citação a `Practical.CleanArchitecture`, `ModularMonolith` ou `ClassifiedAds` no README; o monólito modular, o comando/consulta com dispatcher e o BFF passam a justificar a escolha por si. Options e Strategy citados como avaliados na ADR-0012 (proposta), sem afirmar uso.
- Tabela de testes atualizada para os **147** casos medidos (`./start.sh test`), com a linha do Terminal; lista de ADRs completada (0011–0013).
- Seção de estrutura virou tabela com os **24** links de README de projeto, todos resolvendo; criados os READMEs que faltavam (Blazor.Modules, BlazorServerSide, Terminal, Terminal.Tests, BlazorServerSide.IntegrationTests).

## Exceções registradas

- **verify pulado no fechamento (`--no-verify`), 2026-11-19.** `verify_all.sh` roda `dotnet test --no-build -q`, recusado pelo MTP (exit 5) — defeito de ferramenta das tasks 0010–0017. Mudança só de documentação (READMEs); a suíte permanece 147/147 da task 0016.
