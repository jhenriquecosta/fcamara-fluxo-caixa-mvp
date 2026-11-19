# Task 0011-experiencia-de-demonstracao — Experiência de demonstração: start.sh, UI de terminal e painel de infraestrutura

tipo: feature
branch: feature/0011-experiencia-de-demonstracao
estado: em andamento
worktree: .claude/worktrees/feature-0011-experiencia-de-demonstracao
responsavel: jhenriquecosta@hotmail.com
next_skill: ultron-discover
issue: #27

## Contexto

Diferencial para a banca: uma experiência que prova que **toda** a infraestrutura funciona, não só a API. Duas UIs (web e terminal) sobre o mesmo cliente HTTP, mais um `start.sh` que sobe tudo, roda os testes e mostra o fluxo. Nasce da `feature/0008-frontend-blazor` (precisa do front).

## Escopo

- `start.sh` (padrão ultron-preview, com bloco-manifesto): pré-requisitos → testes → stack → saúde → ELK → painel de acesso; subcomandos para demo, status, logs, terminal e down.
- UI de terminal (.NET, Spectre.Console): lançar, listar/estornar, consolidado, saúde da infraestrutura, métricas do Prometheus, logs do Elasticsearch e a demo de ponta a ponta narrada.
- Painel **Infraestrutura** no front: saúde de cada serviço, sinais do Prometheus, últimos logs do Elasticsearch e atalhos para Grafana, Kibana e Prometheus.
- Os clientes de Prometheus/Elasticsearch/saúde ficam no `Cashflow.Blazor.Modules` e servem às duas UIs.

## Critérios de aceitação

- [ ] `./start.sh` sobe a stack do zero, roda a suíte e termina com o painel de URLs e a saúde de cada serviço
- [ ] `./start.sh demo` executa o fluxo ponta a ponta (lançar → outbox → Kafka → consolidado → estorno → idempotência) narrando cada passo
- [ ] O painel Infraestrutura do front mostra saúde, métricas e logs reais da stack
- [ ] A UI de terminal faz lançar, estornar, consultar consolidado, ver métricas e logs
- [ ] Um serviço fora do ar aparece como fora, sem derrubar o painel nem o terminal
- [ ] Suíte verde
