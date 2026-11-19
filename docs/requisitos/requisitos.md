# PRD — Fluxo de caixa (MVP)

| Campo | Conteúdo |
|---|---|
| **Documento** | PRD-0001 · Product Requirements Document |
| **Projeto** | fcamara-fluxo-caixa-mvp |
| **Autor** | ULTRON · skill ultron-prd — Operador: J Henrique Costa |
| **Data** | 2026-11-18 |
| **Versão do documento** | 1.3 |
| **Status** | accepted |
| **Rastreabilidade** | Desafio técnico Carrefour (via FCamara) · repositório público · sem issue/task |

> **Uma linha:** Dois serviços — Lançamentos e Consolidado — para o comerciante registrar débitos e créditos, estornar sem editar e consultar o saldo diário, com o registro de lançamentos disponível mesmo quando o consolidado cai.

## 1. Problema

O desafio define a necessidade: o comerciante precisa controlar o fluxo de caixa diário com lançamentos de débito e crédito e consultar o saldo diário consolidado. A solução tem de sustentar três obrigações que não são triviais:

- **Registrar uma vez.** Uma repetição de envio — retry de rede, reenvio de um integrador — não pode duplicar o movimento.
- **Corrigir sem apagar.** O estorno precisa corrigir o caixa sem editar o lançamento original; a trilha do que aconteceu permanece.
- **Separar as falhas.** O serviço de controle de lançamento não pode ficar indisponível se o sistema de consolidado diário cair.

A esses requisitos de negócio somam-se duas condições explícitas de operação: o consolidado recebe **50 requisições por segundo** em dias de pico, com **no máximo 5% de perda**, e o serviço de lançamentos continua aceitando movimentos quando o consolidado está fora.

## 2. Solução

Uma solução em dois serviços, com comunicação por eventos:

- **Lançamentos** valida e grava o lançamento, a chave de idempotência e o evento de outbox na mesma transação; publica o evento no Kafka. O estorno é um novo lançamento.
- **Consolidado** consome o evento, ignora repetidos e mantém a projeção diária por comerciante; responde `saldoDia`, `saldoAnterior` e `saldoAcumulado`. A consulta lê só a projeção e nunca chama Lançamentos.

O alvo é AWS (Route 53, ALB/WAF, Cognito, ECS Fargate, RDS PostgreSQL, MSK, CloudWatch, Secrets Manager/KMS), registrado no ADR-010. O MVP roda localmente em docker compose, com Postgres e Kafka, e prova os mecanismos críticos: idempotência, outbox, consumo com DLT e consolidação. A stack do MVP é **.NET 10 LTS** — stack do Carrefour.

## 3. Histórias de usuário

1. Como comerciante, quero registrar um lançamento de débito ou crédito, para que o fluxo de caixa reflita cada movimento do dia.
2. Como comerciante, quero informar uma chave de idempotência ao registrar, para que uma repetição de envio não duplique o lançamento.
3. Como comerciante, quero estornar um lançamento, para corrigir um erro sem apagar o histórico.
4. Como comerciante, quero que o estorno seja um novo lançamento, para manter a trilha auditável do que aconteceu.
5. Como comerciante, quero consultar o saldo do dia, para saber a posição do caixa sem fechar o dia manualmente.
6. Como comerciante, quero ver o saldo anterior e o acumulado junto com o saldo do dia, para entender a evolução do caixa.
7. Como financeiro do comerciante, quero que o consolidado seja derivado dos lançamentos, para não depender de soma manual.
8. Como integrador (ERP ou script), quero autenticar com client credentials e chamar a API, para lançar movimentos automaticamente.
9. Como pessoa usuária, quero entrar com authorization code + PKCE, para operar o fluxo com segurança.
10. Como operador, quero que o serviço de lançamentos continue disponível quando o consolidado cai, para não parar o registro de movimentos.
11. Como operador, quero que uma falha no consumo seja reprocessada e, se persistir, vá para a DLT, para não perder eventos.
12. Como operador, quero ver a idade do outbox, o lag do consumidor, a DLT, a taxa de 5xx e a saturação dos pools, para agir antes de o cliente perceber.
13. Como auditor, quero que cada requisição tenha requestId e traceId nos logs, para rastrear um lançamento de ponta a ponta.
14. Como auditor, quero que um evento repetido seja ignorado no consumo, para que o saldo não seja somado duas vezes.
15. Como avaliador do desafio, quero subir o MVP localmente com um comando e ver o fluxo ponta a ponta, para verificar a solução sem depender de AWS.

## 4. Requisitos funcionais

| ID | Requisito | Comportamento observável |
|---|---|---|
| **RF-1** | Registrar lançamento | `POST /api/v1/lancamentos` com o header `Idempotency-Key`. Corpo: `{ "valor": 100.00, "tipo": "debito" \| "credito", "descricao": "opcional" }`. O `comercianteId` vem do token, nunca do corpo; o **dia do lançamento** é atribuído pelo servidor (dia do registro em `America/Sao_Paulo`), não pelo cliente. Resposta `201` com o id e os dados gravados. `valor > 0`; `tipo` fora do enum é rejeitado (`422`). |
| **RF-2** | Idempotência | Mesma `Idempotency-Key` + mesmo corpo → a resposta original, sem novo lançamento. Mesma chave + corpo diferente → `422 Unprocessable Content`; retry concorrente enquanto o original processa → `409 Conflict`. A chave é reivindicada numa transação própria (commitada antes do trabalho, o que permite o `409` concorrente); o lançamento, o evento e a resposta da chave são gravados na transação do lançamento. |
| **RF-3** | Estornar lançamento | `POST /api/v1/lancamentos/{id}/estornos`, também com `Idempotency-Key`. Cria um **novo lançamento** de tipo oposto, com o mesmo valor, datado no dia do estorno. O original nunca é editado. Cada lançamento admite no máximo um estorno; um segundo → `409`. Um estorno não pode ser estornado (`422`). |
| **RF-4** | Evento de outbox | Na mesma transação do lançamento, grava o evento de outbox. O publicador lê os pendentes em lotes com `FOR UPDATE SKIP LOCKED`, publica no tópico `cashflow.lancamentos.v1` com `acks=all`, chave `comercianteId`, e marca `published_at`. A entrega é at-least-once. |
| **RF-5** | Consumo idempotente | O consumidor valida o schema, verifica o `eventId` em `evento_processado`, ignora repetidos e aplica o valor à projeção do dia na mesma transação. Falha: 1 + 3 tentativas (1 s, 2 s, 4 s) e envio para a DLT. |
| **RF-6** | Consultar consolidado | `GET /api/v1/consolidado/{data}` responde `{ "comercianteId", "data", "saldoDia", "saldoAnterior", "saldoAcumulado" }`. O `comercianteId` vem do token. A consulta lê só a projeção; não chama Lançamentos. Dia sem movimento responde zeros, com o saldo anterior do último dia consolidado. |
| **RF-7** | Autenticação e autorização | Pessoas: OIDC authorization code + PKCE. Integradores: client credentials. O JWT é validado pelas chaves públicas (JWKS); os escopos exigidos são `lancamentos.write` e `consolidado.read`. Requisição sem token válido → `401`; token sem escopo → `403`. |
| **RF-8** | Saúde do serviço | Cada serviço expõe um endpoint de health para o balanceador (`GET /health`), separando liveness e readiness. |
| **RF-9** | Observabilidade | Logs em JSON com `requestId` e `traceId`. Métricas: idade do outbox, lag do consumidor, contagem na DLT, taxa de 5xx e saturação dos pools. Alarmes no alvo AWS. |
| **RF-10** | Execução local | `docker compose up` sobe Postgres, Kafka e os dois serviços, aplica as migrações e cria o tópico. Nenhum recurso AWS é necessário para rodar o MVP. |
| **RF-11** | Testes automatizados | Testes dos caminhos críticos: idempotência, estorno, entrega duplicada, DLT e cálculo do consolidado. Cada teste cita o critério de aceitação que cobre. |
| **RF-12** | Documentação no repositório | README com instruções de execução, este PRD, os ADRs, o blueprint de negócio, a arquitetura-alvo e os documentos dos diferenciais. |

## 5. Requisitos não funcionais

Os requisitos marcados **`(enunciado)`** vêm do desafio. Os marcados **`(proposta)`** são metas que este documento propõe e que o desenho e a validação devem confirmar — não são fatos.

| ID | Requisito | Métrica | Meta | Verificação |
|---|---|---|---|---|
| **RNF-1** | Isolamento de falha entre os serviços `(enunciado)` | Lançamentos aceitos com o consolidado fora | 100% dos lançamentos válidos aceitos no teste | Parar o serviço de consolidado e postar lançamentos |
| **RNF-2** | Desempenho do consolidado `(enunciado)` | Requisições por segundo e taxa de sucesso | 50 req/s com ≥95% de sucesso | Teste de carga local contra a API do consolidado |
| **RNF-3** | Latência `(proposta)` | p95 por endpoint | `POST /lancamentos` ≤ 300 ms; `GET /consolidado` ≤ 200 ms | Teste de carga local |
| **RNF-4** | Disponibilidade `(proposta)` | Uptime mensal | 99,9% lançamentos; 99,5% consolidado | Desenho Multi-AZ; monitoração no alvo |
| **RNF-5** | Recuperação `(proposta)` | RPO e RTO | RPO ≤ 5 min; RTO ≤ 30 min | Desenho de backup e Multi-AZ; validar no alvo |
| **RNF-6** | Consistência | Lançamentos perdidos / efeitos duplicados | 0 e 0 | Testes de idempotência e entrega duplicada |
| **RNF-7** | Segurança | Controles verificáveis | TLS em trânsito; criptografia em repouso; segredos no Secrets Manager; WAF com rate limit; nenhum segredo em código ou imagem | Revisão de desenho; testes de autenticação |
| **RNF-8** | Observabilidade | Métricas e alarmes | Idade do outbox, lag do consumidor, DLT > 0, 5xx, saturação dos pools | Métricas expostas; alarmes no alvo |
| **RNF-9** | Escalabilidade `(proposta)` | Escala horizontal por serviço | Escalar as tasks sem mudar o contrato; partição por `comercianteId` | Desenho; teste local limitado a uma instância |
| **RNF-10** | Portabilidade local | Subida do ambiente | `docker compose up` funciona sem AWS | Execução do README |
| **RNF-11** | Manutenibilidade | Build e testes | `dotnet build` e `dotnet test` verdes | Pipeline local |

## 6. Decisões de implementação

**Estilo arquitetural.** Dois serviços com comunicação assíncrona: transactional outbox no lado de escrita e projeção de leitura no lado do consolidado. A escolha isola a falha do consolidado (RNF-1), permite reprocessar eventos (RNF-6) e mantém a consulta independente da escrita. O custo é a consistência eventual do consolidado e a complexidade operacional do Kafka — aceitos conscientemente.

**Kafka em vez de fila simples.** O MSK foi escolhido no ADR-010 por três razões: ordem por `comercianteId` (a chave da mensagem), replay e visibilidade de lag por consumidor. Uma fila gerenciada simples (SQS) seria mais barata e operacionalmente menor, mas perderia ordem e replay — o trade-off está registrado para a justificativa de tecnologia.

**Banco.** PostgreSQL. Dois bancos lógicos — `lancamentos` e `consolidado` — com usuários próprios por serviço, na mesma instância no MVP. A separação é lógica, não física: uma queda da instância afeta os dois serviços. O RNF-1 é atendido no nível de aplicação (consolidado fora não impede lançamentos); para atendê-lo também no nível de infraestrutura, a evolução é separar as instâncias — registrado no ADR de trade-offs.

**.NET.** ASP.NET Core nos dois serviços, em **.NET 10 LTS** — o .NET 8 e o .NET 9 saíram de suporte em 10/11/2026 (Research #0001). As notas de JDBC do ADR-010 traduzem-se para o pool de conexões do Npgsql; o publicador do outbox e o consumidor rodam como `BackgroundService` no mesmo executável do serviço, separáveis no alvo (como o ADR-010 prevê). Clientes e testes estão detalhados no ADR-0006: Confluent.Kafka 2.16.0, Npgsql 10.0.3 + Dapper 2.1.79, xUnit v3 + Testcontainers.

**Contrato do evento.** Envelope JSON versionado:

```json
{
  "eventId": "uuid",
  "eventType": "lancamento.registrado",
  "occurredAt": "2026-11-18T12:00:00Z",
  "version": 1,
  "payload": {
    "lancamentoId": "uuid",
    "comercianteId": "uuid",
    "tipo": "debito",
    "valor": 100.00,
    "data": "2026-11-18",
    "estornoDe": "uuid-opcional"
  }
}
```

Tópico `cashflow.lancamentos.v1`, chave `comercianteId`, retenção de 30 dias, DLT com a mesma retenção.

**Projeção do consolidado.** `consolidado_diario` guarda, por `(comercianteId, data)`, o `saldoDia` e o `saldoAcumulado` (soma corrente). O `saldoAnterior` é `saldoAcumulado - saldoDia`. A leitura fica O(1) e atende ao RNF-2 sem prefix sum por requisição. `evento_processado` guarda o `eventId` aplicado.

**Estorno.** É um novo lançamento, de tipo oposto, datado no dia do estorno; afeta o consolidado desse dia e não reescreve o dia original. O original permanece imutável. Cada original admite um estorno.

**Idempotência.** `Idempotency-Key` no header, obrigatório; unicidade por `(comercianteId, chave)`; hash do corpo para detectar reuso com payload diferente. A gravação é transacional com o lançamento.

**Costuras de teste.** Duas: a API HTTP dos dois serviços (comportamento externo dos endpoints) e o handler de consumo (aplicação do evento à projeção). Os testes batem nessas costuras; não em detalhes internos.

**Rastreabilidade.** O ADR-010 (`docs/requisitos/Arquietura-solucao-v2.png`) é o desenho-base. As decisões deste PRD que o alteram — estorno datado no dia do evento, separação lógica com instância única, contrato do evento — viram ADRs próprios antes da implementação.

## 7. Decisões de teste

Um bom teste observa comportamento externo — a resposta da API, o efeito na projeção, a mensagem na DLT — e não a implementação interna. Não se testa a classe que grava o outbox; testa-se que o lançamento e o evento existem depois do commit, e que uma falha no meio não deixa um sem o outro.

- **API de lançamentos:** criação, validação, idempotência (mesma chave, corpo igual e diferente), estorno (regras e imutabilidade) e autorização por escopo.
- **Consumidor:** evento novo aplica ao dia; evento repetido não soma de novo; falha persistente vai para a DLT depois das tentativas; evento malformado é rejeitado.
- **Consolidado:** `saldoDia`, `saldoAnterior` e `saldoAcumulado` corretos ao longo de vários dias e comerciantes; dia sem movimento; isolamento entre comerciantes.
- **Falha isolada:** com o consolidado parado, os lançamentos continuam aceitando e o outbox acumula; ao voltar, o consumidor alcança.
- **Carga:** 50 req/s no consolidado com ≥95% de sucesso.
- **Prior art:** não há — o código é novo. A referência são os critérios de aceitação abaixo.

## 8. Avaliação de dependências (SCA)

Sem dependências externas gerenciadas — não há lockfile nesta entrega ainda. A seção passa a valer quando o projeto .NET for criado; nesse momento, rodar `/ultron-deps-audit` e registrar o snapshot.

## 9. Fora de escopo

- Interface gráfica; a entrega é API e documentação.
- Provisionamento da AWS; o alvo é desenho, o que roda é local.
- ERP ou contabilidade completa: notas fiscais, impostos, multimoeda.
- Consolidação em tempo real; o consolidado é diário e eventualmente consistente.
- Lançamento retroativo; o dia do lançamento é o dia do registro, atribuído pelo servidor (ADR-0005).
- Migração de legado em código; a arquitetura de transição é um diferencial documentado.
- Multi-região e recuperação de desastre; RPO/RTO são metas propostas, não validadas.
- Um token representando vários comerciantes; no MVP, um token pertence a um comerciante.

## 10. Observações

- **Pesquisa realizada:** o Research #0001 (18/11/2026) fundamentou a stack (.NET 10 LTS), o barramento (Kafka 4.x em KRaft), o outbox e a semântica de idempotência (422 para payload diferente, 409 para requisição em processamento). As decisões completas estão nos ADRs 0001–0006.
- **Diferenciais do desafio:** arquitetura de transição, estimativa de custos, observabilidade e critérios de segurança para consumo dos serviços entram como documentos próprios.
- **Prazo:** entrega em 20/11/2026 pela manhã.
- **Local do documento:** não há task — este PRD é a entrega de requisitos da solução, e vive em `docs/requisitos/` para ser lido pela banca. O rito de PRD por task não se aplica a uma entrega de solução.
- **Próximos artefatos:** ADRs de justificativa (tradução do ADR-010 para .NET, instância compartilhada, contrato de evento), documento de arquitetura-alvo e README raiz.

## Critérios de aceitação

- [AC1] Quando um lançamento válido é enviado com `Idempotency-Key`, a API responde `201` e o movimento aparece no consolidado do dia.
- [AC2] Quando a mesma `Idempotency-Key` e o mesmo corpo são reenviados, a API devolve a resposta original e não cria um segundo lançamento.
- [AC3] Quando a mesma `Idempotency-Key` é usada com corpo diferente, a API responde `422`.
- [AC4] Quando um lançamento existente é estornado, um novo lançamento de tipo oposto é criado e o original permanece inalterado.
- [AC5] Quando um segundo estorno do mesmo original é pedido, a API responde `409`; estornar um estorno responde `422`.
- [AC6] Quando o evento de um lançamento é entregue duas vezes, o consolidado soma o valor uma única vez.
- [AC7] Quando o consumo falha de forma persistente, o evento passa pelas tentativas e chega à DLT.
- [AC8] Quando o serviço de consolidado é parado, o serviço de lançamentos continua aceitando lançamentos válidos.
- [AC9] Quando o consolidado recebe 50 requisições por segundo em teste local, a taxa de sucesso é de pelo menos 95%.
- [AC10] Quando a consulta do consolidado é feita, a resposta traz `saldoDia`, `saldoAnterior` e `saldoAcumulado` do comerciante e do dia pedidos.
- [AC11] Quando a requisição não tem token válido, a API responde `401`; sem o escopo exigido, `403`.
- [AC12] Quando um comando do README é seguido em uma máquina limpa, o ambiente local sobe e o fluxo ponta a ponta funciona.
- [AC13] Quando um retry concorrente com a mesma `Idempotency-Key` chega enquanto o original ainda processa, a API responde `409`.

---

**Proveniência.** Enunciado do desafio (Descritivo da Solução e Requisitos), ADR-010 (`docs/requisitos/Arquietura-solucao-v2.png`), `docs/ultron/agents/solution.md` (`## Visão` e `## Arquitetura de negócio`), o blueprint de negócio em `docs/ultron/agents/business/`, o Research #0001 (`docs/ultron/agents/research/0001-fatos-2026.md`) e os ADRs 0001–0006 (`docs/ultron/agents/adr/`). Os números de RNF-1 e RNF-2 vêm do enunciado; os de RNF-3 a RNF-5 são metas propostas, marcadas como tal.

**Histórico de versões.**

| Versão | Data | Autor | Mudança |
|---|---|---|---|
| 1.0 | 2026-11-18 | ULTRON · ultron-prd | Criação do PRD de requisitos do MVP |
| 1.1 | 2026-11-18 | ULTRON · ultron-prd | Alinha .NET 10 LTS e a semântica 422/409 ao Research #0001 e ao ADR-0004 |
| 1.2 | 2026-11-18 | ULTRON · ultron-prd | Aceita o ADR-0005 — dia do registro atribuído pelo servidor; retroativo fora de escopo |
| 1.3 | 2026-11-18 | ULTRON · ultron-prd | Alinha o RF-2 à reivindicação em duas fases do ADR-0004 |

**Rastreabilidade.** Desafio técnico Carrefour (via FCamara) › repositório `fcamara-fluxo-caixa-mvp` › sem issue/task › este documento.

<sub>Gerado por ULTRON v2.1.1 · skill ultron-prd · 2026-11-18T20:07:19Z · em conformidade com a Convenção de Documento ULTRON. Documento versionado em git — a proveniência viaja com o projeto.</sub>
