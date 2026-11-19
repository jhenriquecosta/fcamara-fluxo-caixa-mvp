# Cashflow.BackgroundServer

> O host dos trabalhos de fundo: o publicador do outbox (Lançamentos) e o consumidor do barramento (Consolidado).

## Por que existe

Trabalho de fundo dentro da API concorre com as requisições e escala junto com elas. Também faria cada réplica da API publicar o outbox. Separado, ele escala por conta própria, cai sem derrubar a API e fica de fora do caminho do usuário. É o `BackgroundServer` da referência.

## Finalidade

- Com `Modules:Lancamentos:Enabled`: registra o envio de `LancamentoEventPayload` ao Kafka e sobe o `PublishEventWorker`.
- Com `Modules:Consolidado:Enabled`: registra o recebimento do tópico e sobe o consumidor que projeta o saldo.
- Expõe só `/health` e `/metrics` (lag do consumidor, DLT e idade do outbox).

É também aqui que se decide **quem publica e quem consome**: o host liga o broker ao tipo de mensagem, não o módulo.

`BackgroundServerEntryPoint` é só o marcador que o `WebApplicationFactory` dos testes usa.

## Estilo arquitetural e padrões

- **Composition Root** dos trabalhos de fundo.
- **Hosted Services** (.NET Generic Host).
- **Competing Consumers**: réplicas no mesmo consumer group dividem as partições; vários publicadores dividem o outbox pelo `SKIP LOCKED`.

## Configuração

Igual à do WebAPI, mais `MessageBroker:Kafka:BootstrapServers` (tópicos, DLT e grupo têm padrão em `CashflowDefaults`).

## Decisões

- [ADR-0001](../../docs/ultron/agents/adr/0001-dois-servicos-outbox-projecao.md) e [ADR-0005](../../docs/ultron/agents/adr/0005-projecao-consolidado.md).
