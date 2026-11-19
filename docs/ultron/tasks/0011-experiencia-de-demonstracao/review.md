# Revisão — task 0011-experiencia-de-demonstracao [feature]

**Data:** 2026-11-19 · **Branch:** feature/0011-experiencia-de-demonstracao · **Base:** develop
**Perfil de modelo:** review · **Revisor distinto do codificador:** não — AVISO (mesmo agente/modelo; a revisão é por conformidade contra os artefatos da task, não juízo estético)

---

## Veredito

- [ ] ✅ Conforme — sem achados críticos ou médios
- [x] 🟡 Conforme com ressalvas — 1 achado médio registrado e **corrigido antes do PR** (`dbedc2a`)
- [ ] 🔴 Bloqueado — 0 achado(s) crítico(s)

---

## Achados

*Regra: todo achado cita o artefato que sustenta. Sem artefato, não é achado.*

### 🔴 Críticos

Nenhum.

### 🟡 Médios

| # | Achado | Artefato violado | Arquivo/linha |
|---|--------|------------------|---------------|
| 1 | A demo narrada terminava na idempotência e não mostrava "métricas e logs do que acabou de acontecer" | PRD-0004 RF3 | `src/Cashflow.Terminal/TelaDemo.cs` — corrigido em `dbedc2a`: etapas 7/8 (sinais do Prometheus e últimas entradas do Elasticsearch), cada uma degradando sozinha (RF7); teste e execução real contra a stack em 2026-11-19 |

*(Baixos não são reportados — linter e SonarQube cobrem isso.)*

---

## Dimensões conferidas

| # | Dimensão | Status | Nota |
|---|----------|--------|------|
| 1 | Escopo (PRD/slices) | ⚠ → corrigido | RF1–RF7 cobertos após o conserto; S1 (clientes + painel), S2 (terminal), S3 (`start.sh`) entregues |
| 2 | Arquitetura (ADRs) | ✓ | ADR-0011 (clientes no RCL, terminal integrador via `/dev/token`, `start.sh` conduz); ADR-0010 (terminal só conhece o contrato HTTP — ArchTest novo); ADR-0004 (idempotência exercitada na demo real) |
| 3 | Risco de domínio (checklist) | — não aplicável | task sem checklist de integração própria |
| 4 | Observabilidade (logging.md) | ✓ | o terminal não emite log próprio e não imprime token nem dado sensível; o template de índice do ES passou a refletir o documento real do Serilog (`fields.*`), conferido por teste |
| 5 | Escopo em legado (refactor_policy) | — não é legado | repositório novo |
| 6 | Critérios de aceitação | ✓ | os seis marcados na task, cada um com a evidência (suíte 133/133; `./start.sh` do zero; demo real contra a stack) |
| 7 | A saída, não só o cálculo | ✓ | `TestConsole` afirma o texto das telas: ids, "no ar"/"fora", "indisponível", valores, "idempotência confirmada", métricas e logs |

*"—" significa que a dimensão não pôde ser conferida por falta de artefato de referência.
Isso é informação honesta, não uma aprovação.*

---

## Exceções registradas

| Achado | Motivo declarado | Data |
|--------|------------------|------|
| — | — | — |

---

## Resumo para o corpo do PR

```
Revisão ULTRON: 🟡 1 ressalva (corrigida antes do PR)
- RF3: a demo não mostrava métricas e logs do que aconteceu — corrigido em dbedc2a (etapas 7/8)
```

---

*Gerado por `/ultron-review` entre o verify verde e a abertura do PR.
Não aprova e não faz merge — o ULTRON prepara, o humano decide.*
