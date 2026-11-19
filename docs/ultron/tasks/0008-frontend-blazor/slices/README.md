# Fatias — task 0008 (PRD-0003)

Fatias verticais, todas dentro da issue #24 (é um diferencial de uma task só, e três issues a mais só fariam ruído no tracker). Cada fatia termina com a suíte verde e um commit `feat:`.

| # | Fatia | Entrega verificável | Critérios |
|---|---|---|---|
| S1 | **API: listar lançamentos do dia** | `ListarLancamentosDoDiaQuery` + handler (IQuery, Dispatcher), `ILancamentoRepository.ListarDoDiaAsync` com a flag `estornado`, `GET /api/v1/lancamentos?data=`, escopo `lancamentos.read`. Testes de integração: lista só o comerciante do token, marca o estornado, 403 sem `lancamentos.read`, 400 com data inválida | 3, 4 (lado API) |
| S2 | **Frontend: login, perfis e telas** | `Cashflow.Blazor.Modules` + `Cashflow.BlazorServerSide`: login SSR com cookie, logout, `TokenManager` (JWT de 5 min por perfil), `HttpService`, Lançamentos (lista, novo, estorno) e Consolidado. Testes: redirect sem sessão, credencial inválida, perfil consulta barrado em `/lancamentos/novo`, token ausente do HTML, `HttpService` envia bearer e `Idempotency-Key` e traduz `ProblemDetails`, `TokenManager` emite só os escopos do perfil | 1, 2, 5, 6 |
| S3 | **Implantação e guarda-corpos** | Dockerfile + serviço `web` no compose (8090), ArchTest "o front não referencia módulos/domínio", README e `solution.md` atualizados | 7 |
