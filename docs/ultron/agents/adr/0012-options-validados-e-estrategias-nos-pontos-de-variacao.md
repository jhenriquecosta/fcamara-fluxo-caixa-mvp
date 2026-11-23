# ADR-0012 — Options validados na subida e Strategy só nos pontos de variação real

**Status:** proposed
**Data:** 2026-11-19
**Decisores:** J Henrique Costa (Operador) · ULTRON
**Versão do documento:** 1.0
**Relacionados:** ADR-0002 (Kafka), ADR-0005 (projeção, DLT), ADR-0006 (stack), ADR-0009 (monólito modular)
**Análise completa:** [`docs/arquitetura/avaliacao-de-padroes.md`](../../../arquitetura/avaliacao-de-padroes.md)

## Contexto

A configuração é lida por texto em vários pontos (`configuration["Auth:SigningKey"]` em dois arquivos, `GetSection("Modules:...")` repetido nos hosts). Os padrões estão em três lugares, a validação é escrita à mão e espalhada, e os parâmetros operacionais (lote do outbox, esperas, janela de idempotência, tentativas) estão fixos no código.

A arquitetura-alvo traz três variações já documentadas:

- **Identidade:** RF-7 e `seguranca-integracao.md` exigem JWT validado pelo JWKS do Cognito. O código só valida a chave simétrica de desenvolvimento.
- **Broker:** o alvo é o MSK, e o `custos.md` registra o SQS FIFO como alternativa que corta o custo pela metade.
- **Segredos:** o alvo usa Secrets Manager.

A resiliência está em dois laços de retry escritos à mão, com políticas diferentes e sem jitter.

## Decisão

Aplicar padrão só onde há variação real, e preferir o mecanismo do .NET a uma abstração própria.

1. **Options pattern.** Uma classe por seção (`AuthOptions`, `BusinessTimeOptions`, `MessageBrokerOptions`, opções dos módulos, `OutboxOptions`, `IdempotencyOptions`), registrada com `BindConfiguration`, `ValidateDataAnnotations` e `ValidateOnStart`. Os padrões ficam nos inicializadores, e o `CashflowDefaults` sai.
2. **Strategy na validação do JWT.** `OidcAuthority` (metadados OIDC e JWKS, o alvo) ou chave de desenvolvimento (local), escolhida por `Auth:Authority` × `Auth:SigningKey` via `IConfigureNamedOptions<JwtBearerOptions>`. O `/dev/token` só existe com a estratégia de desenvolvimento. **Implementado na task 0019**, com teste de emissor OIDC falso (metadata + JWKS): token da chave publicada é aceito, token de outra chave recebe 401.
3. **Resiliência com Polly** (`Microsoft.Extensions.Resilience`): pipelines `database` e `consumer`, exponenciais e com jitter, parametrizados pelas opções, no lugar dos dois laços escritos à mão.
4. **Broker:** não implementar um segundo adaptador. `Provider` vira `enum` validado, a escolha fica num ponto só, e as portas documentam as garantias que todo adaptador precisa honrar: ordem por chave, at-least-once e retenção para replay.
5. **Contrato:** os tipos `credito`/`debito` vão para o `Cashflow.Contracts`; o consolidado recusa tipo desconhecido em vez de tratá-lo como débito. **Implementado na task 0021**: `Cashflow.Contracts.TiposDeLancamento`, usado pelos dois módulos, e o `MovimentoDoDia` lança na construção — o consumidor esgota as tentativas e o evento vai para a DLT.

**Não aplicar:** polimorfismo no `TipoLancamento`, resultado que gera a própria resposta HTTP, fuso por comerciante e MediatR.

## Alternativas rejeitadas

| Alternativa | Por que não |
|---|---|
| Continuar lendo a configuração por texto | Cada renomeação é uma busca manual; o erro aparece em runtime, não na subida |
| Interface própria `ITokenValidationStrategy` | Duplica o que o `JwtBearer` do framework já resolve por configuração |
| Laços de retry próprios, só parametrizados | Mantém código próprio que precisa de teste e não traz jitter nem telemetria |
| Registro de provedores de broker e adaptador SQS agora | Um provedor só não justifica o registro; e o SQS FIFO não tem replay, uma garantia que a arquitetura assume (ADR-0002) |
| Classe por tipo de lançamento | Crédito e débito são o domínio, não uma família aberta: mais código sem variação a absorver |

## Consequências

**Positivas**

- Um deploy mal configurado falha na subida; o orquestrador não envia tráfego a ele.
- Ir para o Cognito e para o Secrets Manager vira configuração, não código.
- Some o código de retry próprio; o retry ganha jitter e telemetria.
- O contrato do evento passa a ser tipado dos dois lados.

**Negativas**

- Os `Program.cs`, a `Infrastructure` e o registro dos módulos mudam, e há conflito com a branch do frontend se ela ainda não tiver entrado.
- O Polly entra como dependência (mantida pela Microsoft dentro do `Microsoft.Extensions.Resilience`).

## Plano

Uma task por item, nesta ordem: 1 → 2 → 3 → 4 e 5 juntos, depois da integração do frontend. O comportamento observável não muda; a suíte (77 testes) é a rede de proteção, e cada item acrescenta o teste do que introduz (validação de opções na subida, estratégia OIDC com emissor falso, pipeline de retry).
