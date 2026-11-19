# Task 0019-strategy-de-jwt — Strategy de validação do JWT: OIDC/JWKS por configuração, chave de dev no local

tipo: refactor
branch: refactor/0019-strategy-de-jwt
publicacao: pendente
estado: concluído
worktree: .claude/worktrees/refactor-0019-strategy-de-jwt
responsavel: jhenriquecosta@hotmail.com
next_skill: 
issue: #49

Implementa o item 2 da ADR-0012: duas estratégias atrás do JwtBearer escolhidas por configuração — OidcAuthority (metadados OIDC e JWKS, o alvo Cognito) e ChaveDeDesenvolvimento (local). O /dev/token só existe com a estratégia de desenvolvimento, mesmo em Development. Prova: teste com um emissor OIDC falso (metadata + JWKS) — token assinado pela chave publicada valida; outra chave recusa.

## Validação

- Emissor OIDC falso (`EmissorOidcFalso`, Kestrel em porta livre, metadata + JWKS RSA): com `Auth:Authority`, o token assinado pela chave publicada recebe **200** e o de outra chave recebe **401**; `/dev/token` responde **404** em Development com Authority configurada. Sem Authority, a estratégia de dev segue como antes — toda a suíte antiga continua verde.
- Suíte: **150/150**; `dotnet format --verify-no-changes` limpo. README e ADR-0012 atualizados (item 2 implementado).

## Exceções registradas

- **verify pulado no fechamento (`--no-verify`), 2026-11-19.** `verify_all.sh` roda `dotnet test --no-build -q`, recusado pelo MTP (exit 5) — defeito de ferramenta das tasks 0010–0018. Suíte exercida manualmente: `dotnet test --no-build` → 150/150.
