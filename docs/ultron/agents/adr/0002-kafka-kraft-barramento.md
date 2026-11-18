# 0002. Kafka em KRaft como barramento de eventos

| Campo | Conteúdo |
|---|---|
| **Documento** | ADR-0002 · Architecture Decision Record |
| **Projeto** | fcamara-fluxo-caixa-mvp |
| **Autor** | ULTRON · skill ultron-adr — Operador: J Henrique Costa |
| **Data** | 2026-11-18 |
| **Versão do documento** | 1.1 |
| **Status** | accepted |
| **Rastreabilidade** | Desafio Carrefour (via FCamara) · PRD-0001 · Research #0001 · ADR-010 (desenho-base) |

> **Uma linha:** O barramento é o Kafka, em modo KRaft, com ordem por `comercianteId` — a escolha paga o custo de um cluster sempre ligado em troca de ordem, replay e visibilidade de lag.

## Contexto

A projeção do consolidado precisa reconstruir o saldo a partir dos eventos e não pode perder a ordem por comerciante. O operador precisa enxergar o atraso do consumo e o que falhou. O desenho-base escolheu o Amazon MSK; a pesquisa confirma que Kafka 4.x é **KRaft-only** (o ZooKeeper foi removido no 4.0), que o MSK suporta Kafka 4.0 desde maio/2025 e a migração in-place desde agosto/2026, e que o MSK tem custo fixo relevante em carga baixa (Research #0001, perguntas 3, 9 e 10).

## Decisão

Vamos usar **Kafka 4.x em modo KRaft**: Amazon MSK no alvo e um broker Kafka 4.x em KRaft no docker compose local. O tópico é `cashflow.lancamentos.v1`, com **chave `comercianteId`**, 3 brokers em 3 AZs no alvo, `acks=all`, `min.insync.replicas=2`, retenção de 30 dias e uma DLT com a mesma retenção. O cliente .NET é o **Confluent.Kafka 2.16.0** (ADR-0006).

## Alternativas consideradas

- **SQS FIFO** — mais barato (cobrança por uso, ocioso perto de zero) e com ordem por grupo de mensagens. Rejeitado como alvo: não tem replay nem lag por consumidor, e a reconstrução da projeção depende dos dois. Fica registrado como alternativa de menor custo para um MVP que abrisse mão de replay.
- **SNS + SQS (fan-out)** — adequado para notificar vários consumidores, não para manter um log reprocessável por chave. Rejeitado.
- **RabbitMQ** — entrega e roteamento bons, mas sem retenção longa/replay no modelo que o consolidado precisa, e com custo operacional próprio. Rejeitado.
- **Fila no próprio PostgreSQL** — elimina um componente, mas compete com o banco do caminho crítico e não oferece replay nem lag. Rejeitado.

## Consequências

- **Positivas:** ordem por comerciante; replay para reconstruir a projeção; lag observável; DLT nativa; ecossistema e cliente .NET maduros; portabilidade do desenho.
- **Negativas / trade-offs aceitos:** o MSK é sempre ligado — 3 brokers `kafka.m7g.large` custam cerca de **US$ 447/mês** antes de armazenamento (US$ 0,204/broker-h), e o Serverless tem piso de **US$ 0,75/cluster-h** (Research #0001, pergunta 10); há complexidade operacional de tópico, partições e DLT. O custo entra na estimativa.
- **Desdobramentos:** versionar o schema do evento; monitorar lag da DLT e do consumidor; criar o tópico e a DLT no compose e no alvo; confirmar a versão 4.x do MSK na região escolhida.

---

**Proveniência.** Desafio Carrefour — Requisitos não funcionais; ADR-010; PRD-0001; Research #0001, perguntas 3, 9 e 10; páginas oficiais de preço do MSK, SQS e SNS (consulta de 18/11/2026).

**Histórico de versões.**

| Versão | Data | Autor | Mudança |
|---|---|---|---|
| 1.0 | 2026-11-18 | ULTRON · ultron-adr | Criação |
| 1.1 | 2026-11-18 | ULTRON · ultron-adr | Operador aceita a decisão — status accepted |

**Rastreabilidade.** Desafio Carrefour (via FCamara) › repositório `fcamara-fluxo-caixa-mvp` › PRD-0001 › ADR-0002.

<sub>Gerado por ULTRON v2.1.1 · skill ultron-adr · 2026-11-18T20:27:21Z · em conformidade com a Convenção de Documento ULTRON. Documento versionado em git — a proveniência viaja com o projeto.</sub>
