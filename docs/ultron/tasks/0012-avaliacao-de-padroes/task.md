# Task 0012-avaliacao-de-padroes — Avaliação de padrões: Options (settings) e Strategy nos pontos de variação

tipo: docs
branch: docs/0012-avaliacao-de-padroes
publicacao: pendente
estado: concluído
responsavel: jhenriquecosta@hotmail.com
next_skill: 
issue: #29

Avaliar onde Options pattern e Strategy reduzem dívida técnica futura, com evidência no código e na arquitetura-alvo (Cognito/JWKS, MSK com SQS FIFO como alternativa de custo, Secrets Manager), e onde aplicá-los seria engenharia especulativa. Entrega: docs/arquitetura/avaliacao-de-padroes.md e ADR-0012 proposto. A implementação vem em tasks próprias, depois da decisão do Operador.

## Exceções registradas

- **verify pulado no fechamento (`--no-verify`), 2026-11-19.** O `verify_all.sh` roda `dotnet test --no-build -q`; os projetos usam xunit.v3/Microsoft.Testing.Platform, que recusa `-q` ("Opção desconhecida '--q'", exit 5) — vermelho falso da ferramenta, não do código. Build e lint passaram no próprio verify, e a suíte foi exercida manualmente: `dotnet test --no-build` → 77/77 verdes. O bug do `-q` merece task própria no `ultron-skills`.
