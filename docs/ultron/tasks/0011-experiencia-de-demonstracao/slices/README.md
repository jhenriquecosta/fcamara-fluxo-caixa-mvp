# Fatias — task 0011 (PRD-0004)

Todas dentro da issue #27.

| # | Fatia | Entrega verificável | Critérios |
|---|---|---|---|
| S1 | **Clientes de infraestrutura + painel web** | `Infraestrutura/{Models,Services,Pages}` no RCL; `/infraestrutura` no front com saúde, sinais, logs e atalhos; config no compose. Testes: parse do Prometheus e do ES, serviço fora vira "fora" | 3, 5 |
| S2 | **UI de terminal** | `Cashflow.Terminal` com menu, lançar, estornar, consolidado, métricas, logs e `demo` narrada. Testes com `TestConsole` e API falsa | 2, 4, 5 |
| S3 | **start.sh** | manifesto, `up`/`test`/`demo`/`terminal`/`status`/`logs`/`down`, executado do zero contra a stack. README | 1, 6 |
