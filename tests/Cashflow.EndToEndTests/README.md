# Cashflow.EndToEndTests

> A topologia do compose dentro do processo de teste: duas APIs, o BackgroundServer, PostgreSQL e Kafka reais.

## Por que existe

O valor da arquitetura está no que acontece **entre** os módulos: o lançamento atravessa o outbox, o Kafka e a projeção e aparece no consolidado; um evento repetido não soma duas vezes; uma mensagem envenenada vai para a DLT sem travar o consumo; o consolidado fora do ar não derruba os lançamentos. Nenhum teste de módulo enxerga isso.

## O que cobre

| Classe | Teste | Critério |
|---|---|---|
| `FluxoTests` | débito registrado aparece no consolidado do dia | AC1, AC10 |
| | evento entregue duas vezes soma uma vez só | AC6 |
| | estorno anula o lançamento no consolidado | AC4 |
| `ResilienciaTests` | mensagem que falha em todas as tentativas vai para a DLT | AC7 |
| | lançamentos seguem com o consolidado fora, e o consolidado alcança ao voltar | AC8 |

## Como roda

`Topologia` monta o WebAPI duas vezes (`SoLancamentos`, `SoConsolidado`) e o BackgroundServer, como o compose. PostgreSQL e Kafka 4 (KRaft) em Testcontainers, um de cada por assembly. A projeção é eventual, então as consultas esperam até 40 s pelo saldo (`EsperarConsolidadoAsync`). Precisa de Docker; leva cerca de 35 s.
