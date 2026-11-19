# Task 0011-experiencia-de-demonstracao — Experiência de demonstração: start.sh, UI de terminal e painel de infraestrutura

tipo: feature
branch: feature/0011-experiencia-de-demonstracao
estado: em andamento
worktree: .claude/worktrees/feature-0011-experiencia-de-demonstracao
responsavel: jhenriquecosta@hotmail.com
next_skill: ultron-coder
issue: #27

## Contexto

Diferencial para a banca: uma experiência que prova que **toda** a infraestrutura funciona, não só a API. Duas UIs (web e terminal) sobre o mesmo cliente HTTP, mais um `start.sh` que sobe tudo, roda os testes e mostra o fluxo. Nasce da `feature/0008-frontend-blazor` (precisa do front).

## Escopo

- `start.sh` (padrão ultron-preview, com bloco-manifesto): pré-requisitos → testes → stack → saúde → ELK → painel de acesso; subcomandos para demo, status, logs, terminal e down.
- UI de terminal (.NET, Spectre.Console): lançar, listar/estornar, consolidado, saúde da infraestrutura, métricas do Prometheus, logs do Elasticsearch e a demo de ponta a ponta narrada.
- Painel **Infraestrutura** no front: saúde de cada serviço, sinais do Prometheus, últimos logs do Elasticsearch e atalhos para Grafana, Kibana e Prometheus.
- Os clientes de Prometheus/Elasticsearch/saúde ficam no `Cashflow.Blazor.Modules` e servem às duas UIs.

## Critérios de aceitação

- [x] `./start.sh` sobe a stack do zero, roda a suíte e termina com o painel de URLs e a saúde de cada serviço — executado em 2026-11-19: portas livres, 133/133, 8/8 serviços saudáveis, painel
- [x] `./start.sh demo` executa o fluxo ponta a ponta (lançar → outbox → Kafka → consolidado → estorno → idempotência) narrando cada passo — executado contra a stack: projeção via Kafka, estorno e idempotência confirmados
- [x] O painel Infraestrutura do front mostra saúde, métricas e logs reais da stack — `InfraestruturaTests`, `evidencias/01-painel-infraestrutura.png`
- [x] A UI de terminal faz lançar, estornar, consultar consolidado, ver métricas e logs — `TelaLancarTests`, `TelaLancamentosTests`, `TelaConsolidadoTests`, `TelasDeInfraestruturaTests`; escrita real na demo
- [x] Um serviço fora do ar aparece como fora, sem derrubar o painel nem o terminal — `TelaConsolidadoTests.Servico_fora_*`, `TelasDeInfraestruturaTests.*_com_*_fora_*`
- [x] Suíte verde — `dotnet test`: 133/133 em 2026-11-19

## Fases

- [x] discover — `descoberta.md`
- [x] research — `research.md` (Research #0003, stack local)
- [x] prd — `prd.md` (PRD-0004) · ADR-0011
- [x] slices — `slices/README.md` (S1–S3)
- [x] coder — S1 `cba6e2c`, S2 `be73de8`, S3 `3bd7fd8`
- [x] verify — 133/133; `./start.sh` do zero e demo contra a stack em 2026-11-19

## Exceções registradas

- **verify pulado no fechamento (`--no-verify`), 2026-11-19.** O `verify_all.sh` roda `dotnet test --no-build -q`; com xunit.v3/Microsoft.Testing.Platform o `-q` é recusado ("Opção desconhecida '--q'", exit 5) — vermelho falso da ferramenta, não do código. Suíte exercida manualmente: `dotnet test --no-build` → 133/133; `./start.sh` do zero (8/8 serviços saudáveis) e `./start.sh demo` contra a stack. Mesmo bug registrado na task 0012; conserto pendente no `ultron-skills`.
