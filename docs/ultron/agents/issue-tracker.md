# Issue tracker: GitHub

Issues e PRDs deste repositório vivem como issues no GitHub. Use o `gh` CLI para todas as operações.

> **Pendência:** o repositório ainda não tem remote. Enquanto ele não existir (`gh repo create` ou `git remote add origin <url>`), nenhuma issue pode ser publicada — o `gh` não tem de onde inferir o repositório.

## Convenções

- **Criar uma issue**: `gh issue create --title "..." --body "..."`. Use heredoc para corpos multilinha.
- **Ler uma issue**: `gh issue view <número> --comments`, filtrando comentários por `jq` e trazendo as labels.
- **Listar issues**: `gh issue list --state open --json number,title,body,labels,comments --jq '[.[] | {number, title, body, labels: [.labels[].name], comments: [.comments[].body]}]'` com os filtros de `--label` e `--state` apropriados.
- **Comentar numa issue**: `gh issue comment <número> --body "..."`
- **Aplicar / remover labels**: `gh issue edit <número> --add-label "..."` / `--remove-label "..."`
- **Fechar**: `gh issue close <número> --comment "..."`

Inferir o repositório de `git remote -v` — o `gh` faz isso automaticamente dentro de um clone.

## Pull requests como superfície de triagem

**PRs como superfície de pedidos: não.**

Quando for `sim`, PRs passam pelas mesmas labels e estados que as issues, usando os equivalentes `gh pr`:

- **Ler um PR**: `gh pr view <número> --comments` e `gh pr diff <número>` para o diff.
- **Listar PRs externos para triagem**: `gh pr list --state open --json number,title,body,labels,author,authorAssociation,comments`, mantendo apenas `authorAssociation` `CONTRIBUTOR`, `FIRST_TIME_CONTRIBUTOR` ou `NONE`.
- **Comentar / rotular / fechar**: `gh pr comment`, `gh pr edit --add-label`/`--remove-label`, `gh pr close`.

O GitHub compartilha um único espaço de numeração entre issues e PRs, então um `#42` isolado pode ser qualquer um dos dois — resolva com `gh pr view 42`, com fallback para `gh issue view 42`.

## Quando uma skill diz "publish to the issue tracker"

Crie uma issue no GitHub.

## Quando uma skill diz "fetch the relevant ticket"

Rode `gh issue view <número> --comments`.
