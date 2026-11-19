# Task 0019-strategy-de-jwt — Strategy de validação do JWT: OIDC/JWKS por configuração, chave de dev no local

tipo: refactor
branch: refactor/0019-strategy-de-jwt
estado: em andamento
worktree: .claude/worktrees/refactor-0019-strategy-de-jwt
responsavel: jhenriquecosta@hotmail.com
next_skill: ultron-coder
issue: #49

Implementa o item 2 da ADR-0012: duas estratégias atrás do JwtBearer escolhidas por configuração — OidcAuthority (metadados OIDC e JWKS, o alvo Cognito) e ChaveDeDesenvolvimento (local). O /dev/token só existe com a estratégia de desenvolvimento, mesmo em Development. Prova: teste com um emissor OIDC falso (metadata + JWKS) — token assinado pela chave publicada valida; outra chave recusa.
