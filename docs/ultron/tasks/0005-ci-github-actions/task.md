# Task 0005-ci-github-actions — CI: build e testes no GitHub Actions

tipo: chore
branch: chore/0005-ci-github-actions
publicacao: pendente
estado: concluído
responsavel: jhenriquecosta@hotmail.com
next_skill: 
issue: #17

Workflow de CI que roda build (dotnet build Cashflow.slnx) e a suíte completa (dotnet test --solution Cashflow.slnx: unidade, integração com Testcontainers, ponta a ponta e ArchTests) em todo PR e push contra develop e main. Motivação: o PR #16 foi promovido sem verificação automática; o verify era só local.

## Critérios de aceitação

- [x] `.github/workflows/ci.yml` roda em PR e push contra `develop` e `main`: restore, build Release, `dotnet format --verify-no-changes` e `dotnet test --solution Cashflow.slnx`.
- [x] Os mesmos comandos, rodados localmente em Release, dão verde: build 0 aviso/0 erro, format sem mudança, suíte 57/57.
- [ ] O workflow roda verde no próprio PR desta task (conferido no PR, antes do merge).

## Exceções registradas

- **verify do fechamento pulado (`--no-verify`):** o passo `test` do `verify_all.sh` chama `dotnet test -q`, que o Microsoft.Testing.Platform recusa (ultron-skills#481). O verify foi feito pelos comandos do workflow (critério 2), e o CI deste PR é a verificação independente.
