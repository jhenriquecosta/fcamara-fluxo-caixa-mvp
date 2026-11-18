# Conceitos de informação

Os conceitos que o negócio cria ou consome. Eles alimentam o vocabulário do domínio e são candidatos a agregados no desenho.

Fonte: enunciado do desafio (Descritivo da Solução) e ADR-010; confirmação do Operador em 2026-11-18.

| ID | Conceito | Capacidades | Fonte | Estado |
|---|---|---|---|---|
| INF-1 | Lançamento | CAP-1.1, CAP-1.2 | enunciado do desafio; ADR-010 | confirmada |
| INF-2 | Estorno | CAP-1.2 | enunciado do desafio; ADR-010 | confirmada |
| INF-3 | Comerciante | CAP-1.1, CAP-1.3, CAP-1.4 | enunciado do desafio; ADR-010 (comercianteId) | confirmada |
| INF-4 | Saldo diário consolidado | CAP-1.3, CAP-1.4 | enunciado do desafio; ADR-010 | confirmada |
| INF-5 | Evento de lançamento | CAP-3.1, CAP-3.2 | ADR-010 (tópico cashflow.lancamentos.v1) | confirmada |
| INF-6 | Chave de idempotência | CAP-1.1 | ADR-010 (idempotency_key) | confirmada |
