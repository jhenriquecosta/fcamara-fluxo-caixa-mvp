# Capacidades de negócio — fluxo de caixa do comerciante

O negócio atendido é o do comerciante: controlar o fluxo de caixa diário e conhecer o saldo consolidado. Cada capacidade tem ID estável e é citada pela cadeia de valor, pelos conceitos de informação, pelos donos e pelo lado do software.

Os quatro grupos de nível 1 são os domínios funcionais da solução: **Gestão do fluxo de caixa** (o domínio central), **Gestão de acesso e segurança**, **Integração por eventos** e **Observabilidade e operação** (domínios de apoio).

Fonte de cada linha: o enunciado do desafio (Descritivo da Solução e Requisitos de negócio) e a arquitetura-alvo ADR-010; a confirmação é do Operador em 2026-11-18.

| ID | Capacidade | Fonte | Estado |
|---|---|---|---|
| CAP-1 | Gestão do fluxo de caixa | enunciado do desafio — Descritivo da Solução; confirmada pelo Operador em 2026-11-18 | confirmada |
| CAP-1.1 | Registro de lançamentos de débito e crédito | enunciado do desafio — Requisitos de negócio; ADR-010 (POST /api/v1/lancamentos) | confirmada |
| CAP-1.2 | Estorno de lançamentos | enunciado do desafio; ADR-010 (estorno é um novo lançamento, nunca edição) | confirmada |
| CAP-1.3 | Consolidação diária do saldo | enunciado do desafio — Requisitos de negócio; ADR-010 (consumidor e projeção) | confirmada |
| CAP-1.4 | Consulta de saldos (dia, anterior e acumulado) | enunciado do desafio; ADR-010 (GET /api/v1/consolidado/{data}) | confirmada |
| CAP-2 | Gestão de acesso e segurança | enunciado do desafio — Segurança; ADR-010 (Cognito, WAF, Secrets Manager) | confirmada |
| CAP-2.1 | Autenticação de pessoas | ADR-010 (authorization code + PKCE) | confirmada |
| CAP-2.2 | Autenticação de integradores | ADR-010 (client credentials) | confirmada |
| CAP-2.3 | Autorização por escopo | ADR-010 (JWT com scopes) | confirmada |
| CAP-2.4 | Proteção de borda | ADR-010 (WAF com regras gerenciadas e rate limit por cliente) | confirmada |
| CAP-2.5 | Gestão de segredos e criptografia | ADR-010 (Secrets Manager + KMS) | confirmada |
| CAP-3 | Integração por eventos | ADR-010 (outbox + MSK) | confirmada |
| CAP-3.1 | Publicação de eventos de lançamento | ADR-010 (outbox, acks=all) | confirmada |
| CAP-3.2 | Consumo idempotente de eventos | ADR-010 (consumidor com evento_processado e DLT) | confirmada |
| CAP-4 | Observabilidade e operação | enunciado do desafio — Monitoramento; ADR-010 (CloudWatch) | confirmada |
| CAP-4.1 | Monitoramento e alarmes | ADR-010 (idade do outbox, lag do consumidor, DLT, 5xx, pools) | confirmada |
| CAP-4.2 | Rastreabilidade de requisições | ADR-010 (requestId e traceId nos logs JSON) | confirmada |
