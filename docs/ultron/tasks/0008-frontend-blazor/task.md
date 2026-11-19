# Task 0008-frontend-blazor — Frontend Blazor Server: login, lançamentos, estornos e consolidado

tipo: feature
branch: feature/0008-frontend-blazor
estado: em andamento
worktree: .claude/worktrees/feature-0008-frontend-blazor
responsavel: jhenriquecosta@hotmail.com
next_skill: ultron-coder
issue: #24

## Contexto

Diferencial da entrega (plus para a banca): uma interface web limpa em Blazor Server sobre a API existente, seguindo o `ClassifiedAds.Blazor.Modules` + `ClassifiedAds.BlazorServerSide` da referência Practical.CleanArchitecture.

## Escopo

- Login/logout com autenticação por cookie e autorização por perfil (operador × consulta).
- Lançar crédito/débito com Idempotency-Key gerada pela UI.
- Listar os lançamentos do dia e estornar (estorno é um novo lançamento).
- Consultar o consolidado diário (saldo do dia, anterior e acumulado).
- Endpoint `GET /api/v1/lancamentos?data=` no módulo Lançamentos (o estorno precisa do id).
- Serviço `web` no compose.

## Critérios de aceitação

- [ ] Rota protegida sem sessão redireciona para /login; credencial inválida não autentica
- [ ] Perfil consulta não vê nem acessa lançar/estornar; a API recusa (403) mesmo se chamada
- [ ] Lançamento registrado pela UI aparece na lista do dia
- [ ] Estorno pela UI cria o lançamento inverso e marca o original como estornado
- [ ] Consolidado do dia exibido com saldo do dia, anterior e acumulado
- [ ] O token de acesso nunca chega ao navegador (Blazor Server como BFF)
- [ ] Suíte verde (unitários + integração)

## Fases

- [x] descoberta — `descoberta.md` (10 gaps, decisão autônoma por gap)
- [x] research — `research.md` (Research #0002)
- [x] prd — `prd.md` (PRD-0003) · ADR-0010
- [x] slices — `slices/README.md` (S1–S3)
- [ ] coder
- [ ] verify
