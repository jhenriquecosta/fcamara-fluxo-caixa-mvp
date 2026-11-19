# Cashflow.Terminal

> A segunda UI da demonstração: o mesmo cliente HTTP do front, num menu de terminal.

## Por que existe

A banca precisa ver a infraestrutura funcionando, não só as telas. O terminal prova os mesmos fluxos — lançar, estornar, consolidar — e mostra saúde, métricas do Prometheus e logs do Elasticsearch no mesmo lugar, com a demo narrada de ponta a ponta (ADR-0011).

## Finalidade

- **Menu por perfil**: operador lança e estorna; consulta só lê.
- **Telas**: lançar, lançamentos do dia com estorno, consolidado, saúde dos serviços, métricas e logs — cada uma degrada sozinha quando a fonte cai.
- **Demo narrada**: crédito e débito → outbox → Kafka → consolidado → estorno → reenvio idempotente → métricas e logs do que aconteceu.

## Estilo arquitetural e padrões

- **Um cliente, duas UIs**: consome os mesmos serviços do `Cashflow.Blazor.Modules`; não reimplementa consulta.
- **Strategy na fonte do token**: `DevTokenManager` fala com o `/dev/token` do ambiente local (ADR-0010); com IdP real, só essa peça muda.
- **Degradação por tela**: serviço fora aparece como indisponível, sem derrubar o menu.

## Dependências

Só o contrato HTTP: referencia `Cashflow.Blazor.Modules` e o Spectre.Console; um ArchTest garante que não conhece módulos, domínio nem a infraestrutura do servidor.

## Decisões

- [ADR-0011](../../docs/ultron/agents/adr/0011-superficie-de-demonstracao.md): um cliente, duas UIs e o `start.sh`.
