# 0004. Idempotência por chave e estorno como contra-lançamento

| Campo | Conteúdo |
|---|---|
| **Documento** | ADR-0004 · Architecture Decision Record |
| **Projeto** | fcamara-fluxo-caixa-mvp |
| **Autor** | ULTRON · skill ultron-adr — Operador: J Henrique Costa |
| **Data** | 2026-11-18 |
| **Versão do documento** | 1.2 |
| **Status** | accepted |
| **Rastreabilidade** | Desafio Carrefour (via FCamara) · PRD-0001 · Research #0001 · ADR-010 (desenho-base) |

> **Uma linha:** O cliente manda uma chave de idempotência e recebe a mesma resposta se repetir; o estorno nunca edita o original, é um contra-lançamento datado no dia do estorno.

## Contexto

Retries de rede e reenvios de integradores são normais; sem idempotência, o mesmo movimento entra duas vezes e o saldo mente. Corrigir um lançamento editando-o destrói a trilha de auditoria. A pesquisa mostra que o `Idempotency-Key` está no draft IETF -08 (out/2026), ainda não é RFC, e define a semântica de conflito — `422` para payload diferente e `409` para requisição ainda em processamento (Research #0001, pergunta 6). O consenso de ledger append-only é que a correção é um contra-lançamento, com trava contra estorno duplicado (Research #0001, pergunta 8).

## Decisão

**Idempotência.** Todo `POST` exige o header `Idempotency-Key` (UUID). O banco guarda a chave com **unicidade por `(comercianteId, chave)`** e o hash do corpo. A chave é **reivindicada numa transação própria, commitada antes do trabalho** — é o que permite responder `409` enquanto o original processa; o lançamento, o evento de outbox e a resposta gravada na chave são escritos na transação do lançamento. A reivindicação vale por 30 segundos; depois disso, um retry pode retomá-la. Mesma chave + mesmo corpo → a resposta original, sem novo lançamento. Mesma chave + corpo diferente → **422 Unprocessable Content**. Retry concorrente enquanto o original processa → **409 Conflict**.

**Estorno.** É um **novo lançamento** de tipo oposto, com `estornoDe` apontando para o original e data igual ao dia do estorno. O original **nunca é editado**. Cada original admite no máximo um estorno (unicidade em `estornoDe`); um segundo → **409**. Estornar um estorno → **422**.

**Imutabilidade.** Nenhum `UPDATE` ou `DELETE` na tabela de lançamentos; quando o prazo permitir, um trigger no banco rejeita as duas operações, não só o código da aplicação.

## Alternativas consideradas

- **Sem idempotência, confiando no cliente** — rejeitado: o retry é o caso normal, não a exceção; o saldo duplicaria.
- **Deduplicar pelo hash do corpo** — rejeitado: dois lançamentos legitimamente iguais (mesmo valor, mesmo dia, mesma descrição) seriam tratados como um.
- **Editar o lançamento original** — rejeitado: apaga a trilha e impede auditar o que existia antes.
- **Apagar e recriar** — rejeitado pelo mesmo motivo, com o agravante de quebrar referências.
- **Permitir vários estornos do mesmo original** — rejeitado: o saldo fica ambíguo e a regra de negócio perde sentido.

## Consequências

- **Positivas:** retries seguros; trilha imutável; semântica de erro alinhada ao draft; o integrador sabe exatamente o que aconteceu.
- **Negativas / trade-offs aceitos:** o cliente precisa gerar e guardar o UUID; a tabela de chaves cresce e precisa de retenção; a regra de um estorno por original é uma decisão de negócio que o desafio não explicitou.
- **Desdobramentos:** atualizar o PRD (RF-2 e AC3) de `409` para `422` no payload diferente; documentar o contrato de erros no README; definir a janela de retenção das chaves.

---

**Proveniência.** Desafio Carrefour — Descritivo da Solução; ADR-010 (estorno como novo lançamento); PRD-0001 (RF-1 a RF-3); Research #0001, perguntas 6 e 8; draft IETF `draft-ietf-httpapi-idempotency-key-header-08` (22/10/2026).

**Histórico de versões.**

| Versão | Data | Autor | Mudança |
|---|---|---|---|
| 1.0 | 2026-11-18 | ULTRON · ultron-adr | Criação |
| 1.1 | 2026-11-18 | ULTRON · ultron-adr | Operador aceita a decisão — status accepted |
| 1.2 | 2026-11-18 | ULTRON · ultron-adr | Revisão: esclarece a reivindicação da chave em duas fases (o 409 concorrente) |

**Rastreabilidade.** Desafio Carrefour (via FCamara) › repositório `fcamara-fluxo-caixa-mvp` › PRD-0001 › ADR-0004.

<sub>Gerado por ULTRON v2.1.1 · skill ultron-adr · 2026-11-18T20:27:21Z · em conformidade com a Convenção de Documento ULTRON. Documento versionado em git — a proveniência viaja com o projeto.</sub>
