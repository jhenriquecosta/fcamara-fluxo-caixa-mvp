# Cashflow.Modules.Consolidado.UnitTests

> As regras do saldo diário provadas sem banco nem broker.

## Por que existe

O consolidado é aritmética com consequência: um sinal trocado faz o comerciante ver dinheiro que não tem. As regras do saldo são provadas aqui, isoladas, antes de qualquer SQL.

## O que cobre

| Teste | Regra |
|---|---|
| `Credito_soma_ao_saldo_e_debito_subtrai` | o efeito de cada tipo no saldo |
| `Um_estorno_anula_o_original_no_saldo...` | o estorno chega com o tipo oposto e zera o original |
| `O_saldo_de_abertura_e_o_acumulado_menos_o_movimento_do_dia` | a relação entre os três saldos |
| `Dia_sem_movimento_responde_saldo_do_dia_zero...` | a consulta de um dia vazio carrega o acumulado anterior, com um repositório falso |

## Convenção

A mesma dos outros projetos de teste: nome é a regra, Arrange/Act/Assert, critério em `[Trait("Criterio", "ACn")]`.
