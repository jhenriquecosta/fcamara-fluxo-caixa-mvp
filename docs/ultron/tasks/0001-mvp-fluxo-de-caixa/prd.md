# PRD — MVP de fluxo de caixa (task 0001)

| Campo | Conteúdo |
|---|---|
| **Documento** | PRD-0002 · Product Requirements Document |
| **Projeto** | fcamara-fluxo-caixa-mvp |
| **Autor** | ULTRON · skill ultron-prd — Operador: J Henrique Costa |
| **Data** | 2026-11-18 |
| **Versão do documento** | 1.0 |
| **Status** | draft |
| **Rastreabilidade** | Task 0001 · issue #1 · PRD-0001 (solução) · Research #0001 · ADRs 0001–0006 |

> **Uma linha:** O MVP prova, localmente e com testes, o fluxo de caixa de dois serviços — lançamento com idempotência, estorno imutável, evento por outbox e consolidado diário derivado — em .NET 10, com PostgreSQL e Kafka.

## 1. Problema

O PRD da solução (PRD-0001) define o problema de negócio: o comerciante precisa registrar débitos e créditos uma única vez, corrigir sem apagar o histórico e consultar o saldo diário consolidado. O serviço de lançamentos não pode parar quando o consolidado cai; o consolidado recebe 50 requisições por segundo em pico com no máximo 5% de perda.

Esta task resolve a parte que prova o desenho: um MVP executável localmente que demonstra os mecanismos críticos e os critérios que a banca consegue verificar sem AWS.

## 2. Solução

Dois serviços em .NET 10 — **Lançamentos** e **Consolidado** — comunicando-se por eventos:

- **Lançamentos** valida e grava o lançamento, a chave de idempotência e o evento de outbox na mesma transação; publica no Kafka. O estorno é um novo lançamento.
- **Consolidado** consome o evento, ignora repetidos e mantém a projeção diária por comerciante; responde `saldoDia`, `saldoAnterior` e `saldoAcumulado`. A consulta lê só a projeção.

O ambiente local sobe com docker compose: PostgreSQL, Kafka 4.x em KRaft e os dois serviços. O alvo AWS permanece em desenho (ADR-010 e documentos de arquitetura); nenhum recurso é provisionado. Detalhes de negócio, requisitos não funcionais e critérios completos estão no PRD-0001; as decisões arquiteturais estão nos ADRs 0001–0006, todos aceitos, e a evidência que os fundamenta está no Research #0001.

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
16. Como desenvolvedor, quero subir banco, broker e serviços com um único comando, para verificar o fluxo completo e rodar os testes de integração.
17. Como arquiteto avaliado, quero cada decisão registrada em um ADR rastreável, para que a banca leia o raciocínio por trás da escolha.

## 4. Decisões de implementação

As decisões estruturais estão nos ADRs; esta seção registra o contrato que o MVP implementa.

- **Estilo e barramento (ADR-0001, ADR-0002):** dois serviços, outbox transacional e projeção de leitura; Kafka 4.x em KRaft, tópico `cashflow.lancamentos.v1`, chave `comercianteId`, `acks=all`, DLT com retenção de 30 dias.
- **Banco (ADR-0003):** PostgreSQL, dois bancos lógicos e usuários próprios por serviço; uma instância no MVP.
- **Idempotência e estorno (ADR-0004):** `Idempotency-Key` obrigatório nos POSTs; unicidade por `(comercianteId, chave)` com hash do corpo; `422` para o mesmo par chave + corpo diferente; `409` para concorrência; o estorno é um novo lançamento com `estornoDe`, um por original, e o original nunca é editado.
- **Projeção e dia do registro (ADR-0005):** o dia é atribuído pelo servidor em `America/Sao_Paulo`; lançamento retroativo fica fora do MVP; `consolidado_diario` guarda `saldoDia` e `saldoAcumulado`; `evento_processado` guarda o `eventId`; o consumidor aplica evento e checkpoint na mesma transação.
- **Stack (ADR-0006):** .NET 10 LTS, ASP.NET Core; Confluent.Kafka 2.16.0; Npgsql 10.0.3 + Dapper 2.1.79; outbox próprio com `BackgroundService`; xUnit v3 + Testcontainers; emissor JWT de desenvolvimento no lugar do Cognito.

**Contrato HTTP.** `POST /api/v1/lancamentos` com `Idempotency-Key`; corpo `{"valor": 100.00, "tipo": "debito" | "credito", "descricao": "opcional"}`; o `comercianteId` vem do token; resposta `201` com id, comerciante, dia, valor e tipo. `POST /api/v1/lancamentos/{id}/estornos` com o mesmo header. `GET /api/v1/consolidado/{data}` responde `{"comercianteId", "data", "saldoDia", "saldoAnterior", "saldoAcumulado"}`. `401` sem token; `403` sem escopo.

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

**Costuras de teste.** A API HTTP dos dois serviços e o handler de consumo de eventos. Os testes verificam o comportamento externo nessas duas costuras — não classes internas.

## 5. Decisões de teste

Um bom teste observa comportamento externo: a resposta da API, o efeito na projeção, a mensagem na DLT. Não se testa a classe que grava o outbox; testa-se que o lançamento e o evento existem depois do commit, e que uma falha no meio não deixa um sem o outro.

- **Lançamentos:** criação, validação, idempotência (mesma chave e corpo igual/diferente, concorrência), estorno (regras, imutabilidade, um por original) e autenticação por escopo.
- **Consumidor:** evento novo aplica ao dia; repetido não soma de novo; falha persistente vai para a DLT; evento malformado é rejeitado.
- **Consolidado:** `saldoDia`, `saldoAnterior` e `saldoAcumulado` ao longo de dias e comerciantes; dia sem movimento; isolamento entre comerciantes.
- **Falha isolada:** com o consolidado parado, os lançamentos continuam aceitando e o outbox acumula; ao voltar, o consumidor alcança.
- **Carga:** 50 req/s no consolidado com ≥95% de sucesso.
- **Ferramentas:** xUnit v3, Testcontainers 4.x (PostgreSQL e Kafka) e `WebApplicationFactory`.
- **Prior art:** não há — o código é novo. A referência são os critérios de aceitação abaixo.

## 6. Avaliação de dependências (SCA)

Sem dependências externas gerenciadas no momento — o projeto .NET ainda não existe. A seção passa a valer quando o `packages.lock.json` for criado; nesse momento, rodar `/ultron-deps-audit`.

## 7. Fora de escopo

- Interface gráfica.
- Provisionamento da AWS; o alvo é desenho.
- ERP ou contabilidade completa (notas fiscais, impostos, multimoeda).
- Consolidação em tempo real.
- Migração de legado em código; a transição é um diferencial documentado.
- Multi-região e recuperação de desastre.
- Lançamento retroativo (ADR-0005).
- Um token representando vários comerciantes.

## 8. Observações

- **Pesquisa:** realizada — Research #0001; fundamenta a stack, o barramento, o outbox e a semântica 422/409.
- **Publicação:** o PRD não gera uma nova issue. A task já tem a issue #1 no GitHub, que referencia este PRD e o PRD-0001 da solução.
- **Prazo:** 20/11/2026 pela manhã.
- **Upstream:** PRD-0001 (`docs/requisitos/requisitos.md`) é a fonte dos requisitos de solução; este documento é o recorte da task.

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

**Proveniência.** PRD-0001 (`docs/requisitos/requisitos.md`, v1.2), Research #0001 (`docs/ultron/agents/research/0001-fatos-2026.md`), ADRs 0001–0006 (`docs/ultron/agents/adr/`, accepted), `docs/ultron/agents/solution.md` e o blueprint de negócio em `docs/ultron/agents/business/`.

**Histórico de versões.**

| Versão | Data | Autor | Mudança |
|---|---|---|---|
| 1.0 | 2026-11-18 | ULTRON · ultron-prd | Criação do PRD da task 0001, sintetizado do PRD-0001 |

**Rastreabilidade.** Desafio Carrefour (via FCamara) › repositório `fcamara-fluxo-caixa-mvp` › issue #1 › Task 0001 › PRD-0002.

<sub>Gerado por ULTRON v2.1.1 · skill ultron-prd · 2026-11-18T20:36:32Z · em conformidade com a Convenção de Documento ULTRON. Documento versionado em git — a proveniência viaja com o projeto.</sub>
