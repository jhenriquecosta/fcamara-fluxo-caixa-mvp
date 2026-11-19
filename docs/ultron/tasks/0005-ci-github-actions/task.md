# Task 0005-ci-github-actions — CI: build e testes no GitHub Actions

tipo: chore
branch: chore/0005-ci-github-actions
estado: em andamento
responsavel: jhenriquecosta@hotmail.com
next_skill: ultron-coder
issue: #17

Workflow de CI que roda build (dotnet build Cashflow.slnx) e a suíte completa (dotnet test --solution Cashflow.slnx: unidade, integração com Testcontainers, ponta a ponta e ArchTests) em todo PR e push contra develop e main. Motivação: o PR #16 foi promovido sem verificação automática; o verify era só local.
