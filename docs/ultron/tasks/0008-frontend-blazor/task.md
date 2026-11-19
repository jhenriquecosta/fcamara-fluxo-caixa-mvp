# Task 0008-frontend-blazor — Frontend Blazor Server: login, lançamentos, estornos e consolidado

tipo: feature
branch: feature/0008-frontend-blazor
publicacao: pendente
estado: concluído
worktree: .claude/worktrees/feature-0008-frontend-blazor
responsavel: jhenriquecosta@hotmail.com
next_skill: 
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

- [x] Rota protegida sem sessão redireciona para /login; credencial inválida não autentica — `AutenticacaoTests.AC1_*`
- [x] Perfil consulta não vê nem acessa lançar/estornar; a API recusa (403) mesmo se chamada — `AutenticacaoTests.AC2_perfil_consulta_nao_acessa_lancar`, `PontaAPontaTests.AC2_a_api_recusa_o_perfil_consulta_mesmo_se_a_tela_chamar`
- [x] Lançamento registrado pela UI aparece na lista do dia — `PontaAPontaTests.AC3_*`
- [x] Estorno pela UI cria o lançamento inverso e marca o original como estornado — `PontaAPontaTests.AC3_AC4_lanca_lista_e_estorna_pela_ui`, `ListagemTests.RF10_lista_o_dia_do_comerciante_marcando_o_estornado`
- [x] Consolidado do dia exibido com saldo do dia, anterior e acumulado — `PontaAPontaTests.AC5_*` (o saldo após o evento já é coberto pelo `EndToEndTests`)
- [x] O token de acesso nunca chega ao navegador (Blazor Server como BFF) — `AutenticacaoTests.AC6_o_token_de_acesso_nunca_chega_ao_navegador`
- [x] Suíte verde (unitários + integração) — `dotnet test --solution Cashflow.slnx`: 91/91 em 2026-11-19

## Fases

- [x] descoberta — `descoberta.md` (10 gaps, decisão autônoma por gap)
- [x] research — `research.md` (Research #0002)
- [x] prd — `prd.md` (PRD-0003) · ADR-0010
- [x] slices — `slices/README.md` (S1–S3)
- [x] coder — S1 `5b762d8`, S2 `eaae69d`, S3
- [x] verify — 91/91, imagem `web` construída

## Verificação manual (2026-11-19)

Host rodando local com a API fora do ar, via `curl`: sem sessão → 302 `/login?ReturnUrl=…`; senha errada → 200 sem cookie; `consulta` → 302 `/acesso-negado` em `/lancamentos/novo`; `/lancamentos` mostra "Serviço indisponível" sem derrubar a página. O `docker compose up` **não** foi rodado nesta sessão: o projeto compose `cashflow` é o mesmo da sessão de infra no checkout principal, e subir daqui derrubaria a stack dela. A imagem do `web` foi construída (`docker build`) e o `compose config` valida.

## Verificação no navegador (2026-11-19, após o fechamento)

Stack completa num projeto compose isolado (`docker compose -p cashflow-web`, derrubado com `down -v` ao final, sem tocar no projeto `cashflow` da sessão de infra), percorrida com Playwright. Capturas em `evidencias/`.

| Passo | Resultado |
|---|---|
| `/lancamentos` sem sessão | redireciona para `/login?ReturnUrl=%2Flancamentos` |
| login `operador` | volta para `/lancamentos` |
| crédito R$ 1.500,00, débito R$ 320,50, crédito R$ 80,00 | registrados; aparecem na lista do dia |
| estorno do crédito de R$ 1.500,00 | modal explica o contra-lançamento; original riscado como "estornado", débito "estorno de …" criado, nenhum dos dois estornável; movimento −R$ 240,50 |
| `/consolidado` | saldo do dia −R$ 240,50, anterior R$ 0,00, acumulado −R$ 240,50 — via outbox → Kafka → projeção |
| logout → login `consulta` | lista visível, sem "Novo lançamento" nem "Estornar"; `/lancamentos/novo` → `/acesso-negado` |
| login `loja2` | "Nenhum lançamento" — não vê a loja 1 |
| `consolidado-api` parado | lançamento de R$ 55,00 registrado normalmente; `/consolidado` mostra "serviço indisponível" sem derrubar o circuito |

**Observação não reproduzida:** no primeiro envio do formulário de novo lançamento, o primeiro clique em *Registrar* não teve efeito (nenhum POST à API, nenhum erro no console ou no log); o segundo clique registrou uma vez só (a lista mostrou um único crédito de R$ 1.500,00). Três envios seguintes, inclusive em página recém-carregada, funcionaram ao primeiro clique. Suspeita: corrida do Playwright entre o `fill` e a ligação do circuito interativo; fica registrada para observar na demo.
