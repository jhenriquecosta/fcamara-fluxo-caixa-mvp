# Task 0014-terminal-e-testes-verbosos — Terminal roda e o test mostra os testes passando

tipo: chore
branch: chore/0014-terminal-e-testes-verbosos
publicacao: https://github.com/jhenriquecosta/fcamara-fluxo-caixa-mvp/pull/40
estado: concluído
worktree: .claude/worktrees/chore-0014-terminal-e-testes-verbosos
responsavel: jhenriquecosta@hotmail.com
next_skill: 
issue: #39

Dois defeitos achados no diagnóstico: (1) o prompt do perfil usa [1], que o Spectre lê como markup e derruba o terminal com Unbalanced markup stack; (2) ./start.sh test só mostra o resumo — o reporter verbose do xunit (cada teste STARTING/FINISHED) é recusado pelo handshake do dotnet test, então o test passa a rodar cada projeto pelo próprio runner.

## Causa raiz e correção

- **Terminal:** `TextPrompt` interpreta o texto como markup; `[1]` abre uma tag que nunca fecha. A seleção saiu para `SelecaoDePerfil.Escolher`, com teste no `TestConsole` que reproduz o crash antes da correção; o prompt passou a dizer `(padrão 1)`. Verificado num pty alocado: menu renderiza e sai com 0.
- **`test`:** o handshake do `dotnet test` recusa `-reporter verbose` (mesma família do `-q`); o comando agora compila a solution e roda cada projeto pelo próprio runner com o reporter verbose, imprimindo cada teste e o resumo por projeto. `./start.sh test` → 143 testes, 8 projetos verdes.

## Exceções registradas

- **verify pulado no fechamento (`--no-verify`), 2026-11-19.** O `verify_all.sh` roda `dotnet test --no-build -q`, recusado pelo MTP (exit 5). Suíte exercida manualmente: `./start.sh test` → 143/143 em 2026-11-19. Mesmo bug das tasks 0010–0013; conserto pendente no `ultron-skills`.
