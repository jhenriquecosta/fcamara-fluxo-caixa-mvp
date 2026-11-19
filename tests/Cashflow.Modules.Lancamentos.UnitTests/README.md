# Cashflow.Modules.Lancamentos.UnitTests

> As regras do livro-caixa provadas sem banco, sem broker e sem HTTP. Rodam em milissegundos.

## Por que existe

As regras que mais importam ao negócio (valor positivo, estorno de tipo oposto, estorno de estorno proibido, o dia de São Paulo, o que conta como o mesmo pedido na idempotência) vivem em tipos puros. Testá-las pela API custaria segundos e um container por teste, e a falha apontaria para o HTTP em vez da regra. Aqui cada teste prova uma regra, isolada.

## O que cobre

| Classe | Regra |
|---|---|
| `LancamentoTests` | registro válido; valor zero ou negativo recusado; estorno cria contra-lançamento e não altera o original; estorno de estorno recusado |
| `DiaDeNegocioTests` | 23h30 em São Paulo ainda é o mesmo dia, mesmo já sendo o seguinte em UTC |
| `TipoLancamentoTests` | só `debito`/`credito` (inclusive recusando `"0"` e `"1"`); o oposto de cada tipo |
| `ImpressaoDigitalDoCorpoTests` | o que é o mesmo pedido e o que é outro para a chave de idempotência |

## Convenção

- O **nome do teste é a regra**, em frase: `Estornar_nao_altera_o_lancamento_original`.
- O corpo segue **Arrange / Act / Assert**, marcado quando o teste tem mais de uma linha.
- O critério de aceite do PRD vai em `[Trait("Criterio", "ACn")]`: `dotnet test --solution Cashflow.slnx --filter-trait "Criterio=AC4"` roda, em todos os projetos, só o que prova o AC4.

## Dependências

Só o módulo testado e o xUnit v3. Sem Testcontainers.
