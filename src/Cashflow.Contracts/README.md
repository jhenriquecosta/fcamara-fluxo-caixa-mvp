# Cashflow.Contracts

> O schema do barramento: o envelope do evento, o payload do lançamento, os tipos de evento e os nomes dos tópicos.

## Por que existe

Lançamentos publica e Consolidado consome, e os dois não se conhecem. O que os une é o formato da mensagem. Se esse formato morasse dentro de um dos módulos, o outro teria de referenciá-lo, e a separação acabaria. Ele mora aqui, sozinho: é o contrato, e mudá-lo é uma decisão que afeta os dois lados e ganha versão (`cashflow.lancamentos.v1`).

## Finalidade

- **`CashflowEvent<T>`**: o envelope (`eventId`, `eventType`, `occurredAt`, `version`, `payload`). O `eventId` é a chave de deduplicação do consumidor.
- **`LancamentoEventPayload`**: o que o consolidado precisa para projetar o saldo.
- **`EventTypes`**: `lancamento.registrado` e `lancamento.estornado`.
- **`Topics`**: o tópico, a DLT e o consumer group.
- **`CashflowJson`** e **`CashflowEventSerializer`**: a única serialização, a mesma na API, no outbox e no barramento.

## Estilo arquitetural e padrões

- **Published Language** (DDD estratégico): a linguagem que os contextos usam para conversar.
- **Event-Carried State Transfer**: o evento carrega o que o consumidor precisa; ele não volta para perguntar.
- **Envelope** com versão do schema.

## Dependências

| Pode referenciar | Não pode |
|---|---|
| só a BCL do .NET (`System.Text.Json`) | qualquer projeto da solução |

Provado em `tests/Cashflow.ArchTests` (`Contracts_nao_depende_de_nada`). É isso que permite que outro serviço, em outro repositório, consuma o mesmo tópico só com este pacote.

## Decisões

- [ADR-0002](../../docs/ultron/agents/adr/0002-kafka-kraft-barramento.md): Kafka, partição por comerciante.
- [ADR-0005](../../docs/ultron/agents/adr/0005-projecao-consolidado.md): a projeção idempotente pelo `eventId`.
