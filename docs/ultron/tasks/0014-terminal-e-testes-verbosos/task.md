# Task 0014-terminal-e-testes-verbosos — Terminal roda e o test mostra os testes passando

tipo: chore
branch: chore/0014-terminal-e-testes-verbosos
estado: em andamento
worktree: .claude/worktrees/chore-0014-terminal-e-testes-verbosos
responsavel: jhenriquecosta@hotmail.com
next_skill: ultron-coder
issue: #39

Dois defeitos achados no diagnóstico: (1) o prompt do perfil usa [1], que o Spectre lê como markup e derruba o terminal com Unbalanced markup stack; (2) ./start.sh test só mostra o resumo — o reporter verbose do xunit (cada teste STARTING/FINISHED) é recusado pelo handshake do dotnet test, então o test passa a rodar cada projeto pelo próprio runner.
