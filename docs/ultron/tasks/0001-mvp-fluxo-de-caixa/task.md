# Task 0001-mvp-fluxo-de-caixa — MVP de fluxo de caixa — desafio Carrefour

tipo: feature
branch: feature/0001-mvp-fluxo-de-caixa
slices_fechados: #2
estado: em andamento
responsavel: jhenriquecosta@hotmail.com
next_skill: ultron-coder
issue: #1

## Contexto

Desafio técnico para a vaga de Arquiteto de Soluções do Carrefour (via FCamara). A solução tem dois serviços — Lançamentos e Consolidado — com arquitetura-alvo AWS desenhada e MVP executável localmente.

Artefatos de referência: `docs/requisitos/requisitos.md` (PRD-0001), `docs/ultron/agents/research/0001-fatos-2026.md`, `docs/ultron/agents/adr/0001` a `0006`, `docs/ultron/agents/solution.md` e `docs/ultron/agents/business/`.

## Escopo

- Serviço de Lançamentos: API .NET 10, idempotência por `Idempotency-Key`, outbox transacional, estorno como novo lançamento.
- Serviço de Consolidado: consumidor Kafka idempotente, projeção diária com saldo acumulado, API de consulta.
- docker compose local com PostgreSQL e Kafka 4.x em KRaft.
- Testes dos caminhos críticos (xUnit v3 + Testcontainers).
- README com instruções de execução local.
- Documentos restantes do desafio: arquitetura-alvo em texto, transição, custos, observabilidade e segurança de integração.

## Artefatos do rito

- **Descoberta:** `docs/ultron/agents/solution.md` (`## Visão`) — entrevista de 18/11/2026.
- **Research:** `docs/ultron/agents/research/0001-fatos-2026.md` — fatos de novembro/2026.
- **PRD da task:** `prd.md` (PRD-0002), sintetizado do PRD-0001 (`docs/requisitos/requisitos.md`).
- **ADRs:** 0001–0006, aceitos em `docs/ultron/agents/adr/`.

## Critérios de aceitação

- [x] [AC1] Quando um lançamento válido é enviado com `Idempotency-Key`, a API responde `201` e o movimento aparece no consolidado do dia.
- [x] [AC2] Quando a mesma `Idempotency-Key` e o mesmo corpo são reenviados, a API devolve a resposta original e não cria um segundo lançamento.
- [x] [AC3] Quando a mesma `Idempotency-Key` é usada com corpo diferente, a API responde `422`.
- [ ] [AC4] Quando um lançamento existente é estornado, um novo lançamento de tipo oposto é criado e o original permanece inalterado.
- [ ] [AC5] Quando um segundo estorno do mesmo original é pedido, a API responde `409`; estornar um estorno responde `422`.
- [x] [AC6] Quando o evento de um lançamento é entregue duas vezes, o consolidado soma o valor uma única vez.
- [ ] [AC7] Quando o consumo falha de forma persistente, o evento passa pelas tentativas e chega à DLT.
- [ ] [AC8] Quando o serviço de consolidado é parado, o serviço de lançamentos continua aceitando lançamentos válidos.
- [ ] [AC9] Quando o consolidado recebe 50 requisições por segundo em teste local, a taxa de sucesso é de pelo menos 95%.
- [x] [AC10] Quando a consulta do consolidado é feita, a resposta traz `saldoDia`, `saldoAnterior` e `saldoAcumulado` do comerciante e do dia pedidos.
- [x] [AC11] Quando a requisição não tem token válido, a API responde `401`; sem o escopo exigido, `403`.
- [ ] [AC12] Quando um comando do README é seguido em uma máquina limpa, o ambiente local sobe e o fluxo ponta a ponta funciona.
- [x] [AC13] Quando um retry concorrente com a mesma `Idempotency-Key` chega enquanto o original ainda processa, a API responde `409`.

## Fatias publicadas

| Fatia | Título | Issue | Estado |
|---|---|---|---|
| 1 | Lançamentos ponta a ponta | #2 | fechada (local) |
| 2 | Evento e consolidado do dia | #3 | fechada (local) |
| 3 | Estorno imutável | #4 | aberta |
| 4 | Resiliência, observabilidade e carga | #5 | aberta |
| 5 | Execução local e README | #6 | aberta |
| 6 | Documentação dos diferenciais | #7 | aberta |

## Exceções registradas (escape consciente)

- **Portão verify, fatia 1 (#2), 2026-11-18:** o `verify_all.sh` executa `dotnet test --no-build -q`; o runner do xUnit v3 / Microsoft.Testing.Platform não aceita `-q` (opção desconhecida). A suíte foi executada manualmente com `dotnet test --solution Cashflow.slnx` — **9/9 verdes** — e o build estava verde. O portão volta a valer quando o comando do arsenal for alinhado ao MTP.
