# 0001. Dois serviços com outbox transacional e projeção de leitura

| Campo | Conteúdo |
|---|---|
| **Documento** | ADR-0001 · Architecture Decision Record |
| **Projeto** | fcamara-fluxo-caixa-mvp |
| **Autor** | ULTRON · skill ultron-adr — Operador: J Henrique Costa |
| **Data** | 2026-11-18 |
| **Versão do documento** | 1.1 |
| **Status** | accepted |
| **Rastreabilidade** | Desafio Carrefour (via FCamara) · PRD-0001 · Research #0001 · ADR-010 (desenho-base) |

> **Uma linha:** Lançamentos e Consolidado são serviços separados, ligados por eventos: o lançamento grava o fato e o evento na mesma transação; o consolidado é uma projeção derivada.

## Contexto

O desafio exige dois serviços — controle de lançamentos e consolidado diário — e impõe duas forças em tensão: o registro de lançamentos não pode parar quando o consolidado cai, mas o consolidado precisa derivar do mesmo fato registrado, sem perder nem duplicar. Registrar e consolidar no mesmo processo tornaria a disponibilidade do registro dependente da carga e das falhas do consolidado; registrar e consolidar por chamada síncrona propagaria a falha de um para o outro.

A pesquisa confirma que o padrão de outbox transacional continua a resposta correta para o dual-write e lista as armadilhas que a implementação precisa tratar — entrega ao menos uma vez, consumidor idempotente, ordenação sob falha e retenção da tabela de outbox (Research #0001, pergunta 5). O desenho-base (ADR-010) já escolheu esse caminho.

## Decisão

Vamos implementar **dois serviços** — Lançamentos e Consolidado — comunicando-se **por eventos**, com **transactional outbox** no lado da escrita e **projeção de leitura** no lado do consolidado. O estorno é um novo lançamento; o consolidado nunca edita o passado.

## Alternativas consideradas

- **Monolito único** — rejeitado: contraria o requisito de negócio (dois serviços) e enfraquece o isolamento de falha; a carga do consolidado competiria com o registro no mesmo processo e no mesmo pool.
- **Dois serviços com chamadas síncronas** — rejeitado: a falha do consolidado derrubaria o registro; a latência ficaria acoplada; o RNF explícito do desafio seria violado.
- **Dois serviços com fila simples (SQS)** — rejeitado: perde ordem por comerciante, replay e visibilidade de lag, que o consolidado usa para reconstruir a projeção. O trade-off completo está no ADR-0002.
- **Event sourcing completo** — rejeitado para este escopo: o lançamento é um fato único, sem ciclo de vida de agregado a reconstruir; trocar a fonte da verdade para os eventos exigiria reidratação, versões otimistas, snapshots, upcasting e consistência eventual de todas as leituras, sem um driver de negócio que o pague. A análise completa e os gatilhos de evolução estão no ADR-0008.
- **Outbox + Kafka + projeção** — escolhido. O ledger é append-only e o evento é o fato publicado — o híbrido registrado no ADR-0008.

## Consequências

- **Positivas:** isolamento de falha entre os serviços; atomicidade lançamento + evento; replay para reconstruir o consolidado; trilha auditável; leitura do consolidado O(1); publicador do outbox separável no alvo.
- **Negativas / trade-offs aceitos:** o consolidado é eventualmente consistente; o Kafka adiciona complexidade operacional; a entrega at-least-once exige consumo idempotente; a instância única do RDS continua sendo um domínio de falha compartilhado (ADR-0003).
- **Desdobramentos:** ADR-0002 (Kafka), ADR-0003 (banco), ADR-0004 (idempotência e estorno), ADR-0005 (projeção), ADR-0006 (stack).

---

**Proveniência.** Desafio Carrefour — Descritivo da Solução e Requisitos não funcionais; ADR-010 (`docs/requisitos/Arquietura-solucao-v2.png`); PRD-0001 (`docs/requisitos/requisitos.md`); Research #0001 (`docs/ultron/agents/research/0001-fatos-2026.md`), pergunta 5.

**Histórico de versões.**

| Versão | Data | Autor | Mudança |
|---|---|---|---|
| 1.0 | 2026-11-18 | ULTRON · ultron-adr | Criação |
| 1.1 | 2026-11-18 | ULTRON · ultron-adr | Operador aceita a decisão — status accepted |

**Rastreabilidade.** Desafio Carrefour (via FCamara) › repositório `fcamara-fluxo-caixa-mvp` › PRD-0001 › ADR-0001.

<sub>Gerado por ULTRON v2.1.1 · skill ultron-adr · 2026-11-18T20:27:21Z · em conformidade com a Convenção de Documento ULTRON. Documento versionado em git — a proveniência viaja com o projeto.</sub>
