# Cashflow.Modules.Consolidado.IntegrationTests

> A consulta do consolidado pela API real, contra um PostgreSQL real em container.

## Por que existe

A consulta depende de SQL (o acumulado do último dia anterior) e tem um requisito não funcional (50 req/s com até 5% de falha). Os dois só se provam com banco e host de verdade.

## O que cobre

| Teste | O que prova |
|---|---|
| `Dia_sem_movimento_responde_200...` | dia vazio responde zero com o acumulado anterior, nunca 404 |
| `Data_fora_do_formato...` | 400 para data inválida |
| `Consolidado_api_nao_expoe_a_rota_de_lancamentos` | o deploy como `consolidado-api` não aceita escrita |
| `Consolidado_sustenta_50_requisicoes_por_segundo...` | AC9: 500 requisições em 10 s com taxa de sucesso ≥ 95% |

## Como roda

Mesma `PostgresFixture` do Lançamentos. Precisa de Docker.
