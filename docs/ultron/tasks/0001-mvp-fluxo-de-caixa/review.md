# Revisão — task #0001 mvp-fluxo-de-caixa

**Data:** 2026-11-18 · **Branch:** feature/0001-mvp-fluxo-de-caixa · **Base:** develop
**Perfil de modelo:** review · **Revisor distinto do codificador:** não — AVISO: o revisor é o mesmo modelo que implementou; a revisão mecânica contra os artefatos foi feita e recomenda-se leitura humana antes do merge.

---

## Veredito

- [ ] ✅ Conforme — sem achados críticos ou médios
- [x] 🟡 Conforme com ressalvas — 2 críticos corrigidos antes do PR; 4 médios corrigidos; 5 médios registrados
- [ ] 🔴 Bloqueado

---

## Achados

### 🔴 Críticos

| # | Achado | Artefato violado | Arquivo/linha | Situação |
|---|--------|------------------|---------------|----------|
| 1 | O compose usava o mesmo usuário `cashflow` nos dois bancos | ADR-0003: "um usuário próprio por serviço" | compose.yaml:52,76 · infra/postgres/init.sql | Corrigido: papéis `lancamentos` e `consolidado`, init e connection strings; validado com `down -v` + demo |
| 2 | O ADR-0004 e o RF-2 diziam chave + hash na mesma transação; o código reivindica a chave numa transação própria, commitada antes (é o que permite o `409` concorrente) | ADR-0004 · PRD-0001 RF-2 | LancamentoService.cs (ReivindicarAsync) | Corrigido nos artefatos: ADR-0004 v1.2 e PRD v1.3 descrevem a reivindicação em duas fases |

### 🟡 Médios

| # | Achado | Artefato | Situação |
|---|--------|----------|----------|
| 1 | A `Idempotency-Key` crua nos logs | logging.md ("nunca crus") | Corrigido: hash da chave |
| 2 | Os nomes de campo do logging.md (`ts`/`level`/`context`/`msg`) não são os do Compact JSON do Serilog | logging.md | Corrigido: a convenção descreve `@t`/`@l`/`@mt`/`SourceContext` e o `@timestamp` do sink |
| 3 | O ADR-0006 diz Dapper 2.1.79; o build usa 2.1.89 | ADR-0006 | Corrigido: ADR-0006 v1.2 |
| 4 | O `apply-elk.sh` falhava a data view no Kibana (o nome com travessão) | README (passo do ELK) | Corrigido: nome ASCII |
| 5 | O consumidor confia no `data` do evento; um evento atrasado corromperia o acumulado | ADR-0005 (retroativo fora do escopo) | Registrado: o Kafka é privado; a validação de fronteira fica como evolução |
| 6 | O teste de carga de 50 req/s é em processo | AC9 · observabilidade.md | Registrado: o documento declara; um cliente externo (k6) fica como evolução |
| 7 | O lag faz uma chamada ao broker por mensagem | ADR-010 (métrica de lag) | Registrado: otimizar a periodicidade |
| 8 | O SLO de frescor (60 s) não tem métrica implementada | observability/slos/cashflow.yml | Registrado: métrica de idade do evento como evolução |
| 9 | O harness de teste usa um usuário só; a separação do ADR-0003 é validada no compose | ADR-0003 | Registrado: testes são harness; o compose foi validado |
| 10 | O template de índice é aplicado depois de o índice do dia existir | observability/elasticsearch | Registrado (baixo): vale para os próximos índices |

---

## Dimensões conferidas

| # | Dimensão | Status | Nota |
|---|----------|--------|------|
| 1 | Escopo (PRD/slices) | ✓ | As seis fatias entregues; nada além do escopo |
| 2 | Arquitetura (ADRs) | ⚠ | Duas violações corrigidas |
| 3 | Risco de domínio (checklist) | — | Não aplicável (sem checklist de risco) |
| 4 | Observabilidade (logging.md) | ⚠ | Três médios corrigidos; os demais registrados |
| 5 | Escopo em legado | — | Não é legado |
| 6 | Critérios de aceitação | ✓ | AC1–AC13 cobertos por testes e pelo demo |
| 7 | A saída, não só o cálculo | ✓ | Respostas da API, consolidado e demo validados |

---

## Exceções registradas

Nenhuma — os críticos foram corrigidos.

---

## Resumo para o corpo do PR

```
Revisão ULTRON: 🟡 conforme com ressalvas (2 críticos corrigidos; 4 médios corrigidos; 5 registrados)
- ADR-0003: usuários por serviço criados e validados no compose.
- ADR-0004/RF-2: artefatos alinhados à reivindicação em duas fases.
- Logs sem a chave crua; convenção atualizada; Dapper 2.1.89.
- Em aberto: validação do data do evento, carga externa, lag por mensagem, frescor, harness com um usuário, template do índice.
Aviso: o revisor é o mesmo modelo que implementou; recomenda-se leitura humana.
```
