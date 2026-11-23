# 0005. Projeção do consolidado com saldo acumulado e dia do registro

| Campo | Conteúdo |
|---|---|
| **Documento** | ADR-0005 · Architecture Decision Record |
| **Projeto** | fcamara-fluxo-caixa-mvp |
| **Autor** | ULTRON · skill ultron-adr — Operador: J Henrique Costa |
| **Data** | 2026-11-18 |
| **Versão do documento** | 1.1 |
| **Status** | accepted |
| **Rastreabilidade** | Desafio Carrefour (via FCamara) · PRD-0001 · Research #0001 · ADR-010 (desenho-base) |

> **Uma linha:** O consolidado é uma projeção por comerciante e dia, com o acumulado guardado para leitura O(1) — e o dia do lançamento é o dia do registro, não uma data retroativa que exigiria reescrever o passado.

## Contexto

A consulta do consolidado recebe 50 requisições por segundo em pico, com no máximo 5% de perda, e não pode chamar o serviço de lançamentos. O saldo precisa ser derivado dos lançamentos, não digitado. A pesquisa confirma que a projeção precisa de idempotência por `event_id`, checkpoint na mesma transação, monitoração de lag e possibilidade de rebuild — e que o CQRS só se justifica com um driver medido, que aqui existe: leitura muito maior que escrita e formato de consulta diferente (Research #0001, pergunta 7).

A entrada de dados tem um detalhe que muda o desenho: se um lançamento puder ser datado no passado e chegar depois do fechamento daquele dia, o consolidado do dia anterior e o acumulado de todos os dias seguintes teriam de ser recalculados. O desafio não pede lançamento retroativo; o fluxo de caixa é diário e o registro acontece quando o movimento acontece.

## Decisão

**Dia do registro.** O dia do lançamento é o **dia do registro no fuso do comerciante** (`America/Sao_Paulo`), atribuído pelo servidor — o cliente não escolhe a data. **Lançamento retroativo fica fora do escopo do MVP.** O estorno segue a mesma regra: é datado no dia em que acontece.

**Projeção.** `consolidado_diario` guarda, por `(comercianteId, data)`, o `saldoDia` e o `saldoAcumulado` (soma corrente). O consumidor aplica o evento e o `eventId` em `evento_processado` **na mesma transação**. O `saldoAnterior` é `saldoAcumulado - saldoDia`; o `saldoAcumulado` é `saldoAnterior + saldoDia`. A API lê só a projeção. Dia sem movimento responde zeros, com o saldo anterior do último dia consolidado. A projeção é reconstruível por replay.

**Chegada tardia não é lançamento retroativo.** O dia do movimento é sempre o do registro, mas o evento pode chegar ao consumidor depois que o dia seguinte já foi aberto: na virada da meia-noite, com o outbox atrasado alguns segundos, ou no reprocessamento de um evento da DLT. A ordem por partição (`comercianteId`) não protege nenhum dos dois casos. Por isso a projeção, na mesma transação, soma o efeito do movimento ao `saldoAcumulado` dos dias posteriores que já existirem. O custo é uma atualização por dia aberto depois do movimento — zero no caso normal, poucos dias no pior caso — e não exige o mecanismo de recomputação que o lançamento retroativo exigiria.

## Alternativas consideradas

- **Calcular o saldo na leitura, somando os lançamentos** — rejeitado: acopla a leitura ao serviço de escrita e compromete os 50 req/s.
- **Guardar só o `saldoDia` e calcular o acumulado na leitura (prefix sum)** — mais simples, mas o custo cresce com o número de dias e a consulta deixa de ser O(1); com o acumulado guardado, a leitura é uma linha.
- **Aceitar data retroativa e recalcular o sufixo de dias** — rejeitado no MVP: exige reconstrução de dias passados e um mecanismo de recomputação que não é necessário para o caso; a evolução fica registrada.
- **Event sourcing completo** — overkill: o desafio pede projeção, não a reconstrução de todo o estado a partir de eventos; o outbox já dá o replay necessário.

## Consequências

- **Positivas:** leitura O(1) e independente da escrita; projeção reconstruível por replay; dia sem movimento tem resposta determinística; o caminho do consumidor é idempotente.
- **Negativas / trade-offs aceitos:** o consolidado é eventualmente consistente e pode ficar atrás do lançamento; não há lançamento retroativo; o acumulado é uma coluna derivada que precisa ser recalculada se a projeção for reconstruída.
- **Desdobramentos:** atualizar o RF-1 do PRD (a data deixa de vir do cliente e passa a ser do servidor) quando este ADR for aceito; monitorar o lag; documentar o fuso do comerciante no README; guardar o `eventId` aplicado.

---

**Proveniência.** Desafio Carrefour — Requisitos não funcionais (50 req/s, 5% de perda); ADR-010 (projeção `consolidado_diario` e `evento_processado`); PRD-0001 (RF-6); Research #0001, pergunta 7.

**Histórico de versões.**

| Versão | Data | Autor | Mudança |
|---|---|---|---|
| 1.0 | 2026-11-18 | ULTRON · ultron-adr | Criação |
| 1.1 | 2026-11-18 | ULTRON · ultron-adr | Operador aceita a decisão — status accepted |
| 1.2 | 2026-11-23 | ULTRON · task 0020 | Chegada tardia: a projeção propaga o efeito aos dias posteriores já abertos (revisão pós-banca) |

**Rastreabilidade.** Desafio Carrefour (via FCamara) › repositório `fcamara-fluxo-caixa-mvp` › PRD-0001 › ADR-0005.

<sub>Gerado por ULTRON v2.1.1 · skill ultron-adr · 2026-11-18T20:27:21Z · em conformidade com a Convenção de Documento ULTRON. Documento versionado em git — a proveniência viaja com o projeto.</sub>
