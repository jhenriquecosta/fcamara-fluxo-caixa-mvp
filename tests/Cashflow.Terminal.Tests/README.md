# Cashflow.Terminal.Tests

> A UI de terminal exercitada de fora: prompt, menu e telas lidos pelo `TestConsole`, com uma API falsa.

## Por que existe

Uma TUI derruba por detalhes que teste de lógica não vê — o `[1]` de um prompt virou tag de markup e matou o app em runtime, sem nenhum teste falhar (task 0014). Aqui cada tela é dirigida com entrada real no console de teste e o texto que o usuário veria é afirmado.

## O que cobre

| Área | O que prova |
|---|---|
| Menu | o perfil consulta não vê lançar; o operador vê |
| Lançar | o POST leva `Idempotency-Key` e o id criado aparece; consulta não chama a API |
| Lançamentos | a marca de estornado aparece e o estorno vira POST do id escolhido; consulta fica em somente leitura |
| Consolidado | os três saldos aparecem; serviço fora vem como indisponível |
| Infraestrutura | saúde, métricas e logs renderizam; cada fonte fora degrada sozinha |
| Demo | o roteiro narra 8 passos e confirma a idempotência com a mesma chave |
| Perfil e token | Enter mantém o operador, `2` escolhe consulta; o `DevTokenManager` pede e reaproveita o token |

## Princípio

O terminal roda com os clientes **reais** sobre um `HttpMessageHandler` falso que registra o que saiu; a asserção é sobre o comportamento observável — a saída do console e a requisição enviada.
