# ADR-0008 — Event sourcing versus ledger append-only com outbox

**Status:** proposed
**Data:** 2026-11-18
**Decisores:** J Henrique Costa (premissa) · ULTRON (avaliação)
**Versão do documento:** 1.0
**Relacionados:** ADR-0001 (dois serviços com outbox e projeção), ADR-0004 (idempotência e estorno), ADR-0005 (projeção e dia do registro), Research #0001 (perguntas 7 e 8)

## Contexto

A decisão original (ADR-0001) escolheu dois serviços com **transactional outbox, Kafka e projeção**, e o ADR-0004 reforçou o **ledger append-only** com contra-lançamento. O Operador levanta a premissa: *em processos bancários e financeiros, event sourcing é a decisão mais adequada* — e a crítica procede em parte: o ADR-0001 **não listou o event sourcing entre as alternativas**. Esta é a avaliação que faltou.

## O que o desenho atual já é

Antes de comparar, é preciso reconhecer o que já está no código:

| Propriedade | Onde |
|---|---|
| Ledger append-only — nenhum `UPDATE`/`DELETE`; a correção é contra-lançamento | `lancamento` (ADR-0004), tabela imutável por regra |
| Fato completo e auditável — valor, tipo, dia, comerciante, `criado_em`, `estorno_de` | linha do ledger |
| Evento versionado e publicado de forma atômica com o fato | outbox transacional, envelope `CashflowEvent<T>` com `version` |
| Leitura derivada e reconstruível | projeção `consolidado_diario` + `evento_processado` |
| Deduplicação e idempotência | chave de idempotência + `evento_processado` |
| Rastreabilidade | `requestId`/`traceId` + `eventId` |

Ou seja: o desenho **não é "o mais simples"** — é o padrão **CQRS + ledger + event log**. O evento existe, é versionado e é reproduzível (o Kafka e o outbox guardam o log; a projeção pode ser reconstruída do ledger). O que o event sourcing muda é **qual artefato é a fonte da verdade**: hoje a linha do ledger é a verdade e o evento é o fato publicado; no ES o evento é a verdade e a linha vira projeção.

## As premissas, uma a uma

### 1. "Processos financeiros → event sourcing"

A afirmação confunde duas camadas do domínio financeiro:

- **O fato contábil** — o lançamento, o journal, o razão. O padrão é o **livro imutável**: cada movimento é um fato, a correção é um contra-lançamento, o saldo é derivado. É o que temos.
- **O estado do produto/da conta** — o ciclo de vida: limite, cheque especial, bloqueios, liquidação, posições, juros e tarifas acumulados. Aqui o estado é uma **máquina de estados rica**, reconstruída de muitos eventos por agregado. Aqui o event sourcing é, de fato, o padrão mais forte — e é o que os core ledgers modernos usam.

O fluxo de caixa do comerciante é a primeira camada. O "agregado" é o próprio movimento: um fato, sem transição de estado a reconstruir. A premissa vale para o **core bancário**, não automaticamente para um **livro de caixa**.

### 2. "A decisão mais simples"

O desenho tem dois serviços, entrega ao menos uma vez, idempotência com 422/409, DLT com 1+3 tentativas, projeção reconstruível, isolamento de falha testado e 50 req/s medidos. O ES **adicionaria** a reidratação de agregado, as versões otimistas, os snapshots, o *upcasting* de eventos e a consistência eventual de **todas** as leituras — inclusive a leitura do próprio lançamento recém-criado. É mais mecanismo, não menos. A escolha foi pelo **driver**, não pelo conforto.

### 3. Os drivers do event sourcing, contra o escopo

| Driver clássico do ES | Presente no desafio? |
|---|---|
| Agregado com muitas transições de estado e invariantes entre eventos | Não — o lançamento é um fato único; a invariante é valor/tipo/um estorno (coberta por `UNIQUE` e regra) |
| Consulta temporal "como estava no instante T" | Não — o negócio pede o saldo **do dia** (a projeção) e o histórico dos lançamentos (o ledger) |
| Múltiplos modelos de leitura divergentes | Não — dois: lançamentos e saldo diário |
| O evento como linguagem primária do negócio | Parcialmente — o evento é o contrato de integração, não a interface do comerciante |
| Reconstrução do estado a partir do log | Não — a projeção é derivada do ledger e reconstruível; o log já existe |
| Auditoria como preocupação primária | **Sim, mas já coberta** — o ledger imutável + o log de eventos + a trilha de requisição entregam o "o quê, quando, por quem"; o ES entregaria a reconstrução do estado, não a auditoria em si |

### 4. O que o ES custa neste escopo

- **Consistência eventual em todas as leituras** — inclusive "meu lançamento foi criado". Hoje a resposta 201 sai com o fato já persistido; no ES puro a leitura depende da projeção.
- **Evolução de schema** — os eventos vivem para sempre; o *upcasting* entra no caminho crítico.
- **Retenção e privacidade** — eventos imutáveis versus direito ao esquecimento; o *crypto-shredding* vira necessário.
- **Operação** — snapshots, compactação, ferramentas (Marten, EventStoreDB, ou artesanal), e a maturidade do time.
- **Prazo** — a entrega é de dois dias; o ES multiplicaria o esforço sem mudar os RNFs do desafio (50 req/s, isolamento de falha), que a projeção já atende.

## A decisão proposta

**Manter o desenho atual para este escopo** — ledger append-only como fonte da verdade, evento versionado publicado por outbox e projeção derivada — e **registrar o event sourcing como evolução com gatilhos explícitos**, não como troca de opinião:

1. O agregado ganhar ciclo de vida: limite, cheque especial, bloqueio, agendamento, parcelamento.
2. O negócio pedir "como estava no instante T" além do saldo do dia.
3. Surgirem modelos de leitura divergentes (extrato analítico, aging, conciliação, relatórios regulatórios).
4. O evento virar a linguagem primária do produto e a auditoria exigir a reconstrução do estado, não só o histórico de fatos.
5. Multimoeda, juros e tarifas acumulados entrarem no escopo.

**Caminho incremental quando o gatilho disparar:** o ledger vira a projeção de um **event store de lançamentos** (a tabela de eventos com versão por agregado, o `lancamento` reconstruído, o contrato da API inalterado), preservado o outbox para a integração — um ADR-0009 na ocasião, com a migração dos dados e a convivência das duas leituras.

## Alternativas consideradas

- **Event sourcing completo agora** — rejeitado: nenhum dos drivers está presente; o custo (reidratação, versões, snapshots, consistência de leitura, upcasting, retenção, ferramental) não se paga em um livro de caixa de fato único, e o prazo do desafio não comporta.
- **Event sourcing só no consolidado** — não muda nada: o consolidado já é uma projeção derivada do log de eventos; o que o ES adicionaria (a fonte da verdade ser o evento) já está no desenho pela via do outbox.
- **Híbrido — o atual** — escolhido: o log de eventos é de primeira classe (versionado, reproduzível, reconstruível), mas a fonte da verdade do fato é o ledger imutável. O melhor dos dois para este escopo.

## Consequências

- O ADR-0001 passa a listar o event sourcing entre as alternativas, citando esta análise.
- O desenho fica documentado como **CQRS + ledger + outbox**, não como "a opção simples".
- A evolução tem gatilhos e um caminho incremental definidos — a decisão deixa de ser opinião e passa a ter data de revisão por evento.
- A auditoria não perde: o ledger, o log e a trilha já cobrem o requisito; o que o ES acrescentaria é a reconstrução de estado, não exigida hoje.

## Histórico

| Versão | Data | Autor | Mudança |
|---|---|---|---|
| 1.0 | 2026-11-18 | ULTRON · ultron-adr | Avaliação do event sourcing contra o ledger com outbox; proposta de manter com gatilhos de evolução |
