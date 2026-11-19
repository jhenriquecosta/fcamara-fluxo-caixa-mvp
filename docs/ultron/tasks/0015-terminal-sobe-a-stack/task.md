# Task 0015-terminal-sobe-a-stack — O terminal sobe a stack quando ela está fora do ar

tipo: chore
branch: chore/0015-terminal-sobe-a-stack
publicacao: https://github.com/jhenriquecosta/fcamara-fluxo-caixa-mvp/pull/42
estado: concluído
worktree: .claude/worktrees/chore-0015-terminal-sobe-a-stack
responsavel: jhenriquecosta@hotmail.com
next_skill: 
issue: #41

O ./start.sh terminal (e o demo) hoje recusam quando a stack está fora, cobrando um up que o Operador precisa lembrar. O ajuste: quando a stack não estiver no ar, o próprio comando terminal/demo roda o up sem a suíte (pré-requisitos, stack, saúde, ELK, painel) e segue para a UI.

## Validação

- `./start.sh down` e, em seguida, `./start.sh terminal` num pty: avisou que a stack estava fora, subiu pelo caminho do up sem a suíte, imprimiu o painel e abriu o prompt/menu — exit 0. `demo` usa o mesmo `garante_stack`.
- Mudança só em `start.sh` e README; nenhum código C# tocado (suíte permanece 143/143 da task 0014).

## Exceções registradas

- **verify pulado no fechamento (`--no-verify`), 2026-11-19.** O `verify_all.sh` roda `dotnet test --no-build -q`, recusado pelo MTP (exit 5) — defeito de ferramenta já registrado nas tasks 0010–0014. Nesta task não há código C# alterado; a validação é o próprio fluxo do script, exercitado acima.
