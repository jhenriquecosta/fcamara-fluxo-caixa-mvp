# Solução — fcamara-fluxo-caixa-mvp

> O mapa da solução: o que é, os repositórios que a implementam, os trackers e
> as convenções. Escrito pelo `/ultron --config`.

nome: fcamara-fluxo-caixa-mvp
o_que_e: MVP de fluxo de caixa: lançamentos, estornos e consolidado diário
tracker_planejamento: github
host_codigo: github
tipos: projeto=Project, milestone=Milestone, task=Issue
fallback_projeto: issue-pai
disciplina: guiado
trilha: gitflow
producao: main
integracao: develop
ofertas: modelos=recusada
tracker: github

## Repos

## Visão

**Contexto.** Este repositório é a entrega do desafio técnico para a vaga de Arquiteto de Soluções do Carrefour (processo via FCamara). O que se avalia não é volume de código: é a capacidade de decompor o problema, decidir com trade-offs explícitos, justificar ferramentas e provar os pontos difíceis. Entrega em **20/11/2026 pela manhã**.

**O problema.** Um comerciante precisa controlar o fluxo de caixa diário — lançamentos de débito e crédito — e consultar o saldo diário consolidado. O serviço de lançamentos não pode ficar indisponível se o consolidado cair; em dias de pico, o consolidado recebe **50 requisições por segundo** com **no máximo 5% de perda**.

**Para quem.** O comerciante (usuário final do fluxo), os sistemas que o integram — ERP, scripts, navegador — e a banca avaliadora do Carrefour, que lê a entrega como demonstração de arquitetura.

**O que é.** Uma solução de fluxo de caixa em dois serviços — **Lançamentos** e **Consolidado** — com arquitetura-alvo na AWS e MVP executável localmente em docker compose. Lançamentos grava o lançamento, a chave de idempotência e o evento de outbox na mesma transação, e publica o evento no Kafka; o Consolidado consome o evento, ignora repetidos e mantém a projeção diária por comerciante. O consolidado é derivado, nunca editado: **estorno é um novo lançamento**. O MVP é em **.NET** (stack do Carrefour).

**Capacidades.** Registrar lançamentos (débito/crédito) · estornar sem editar · autenticar pessoas (authorization code + PKCE) e integradores (client credentials) · publicar eventos com entrega ao menos uma vez · consolidar por comerciante e dia · consultar saldo do dia, anterior e acumulado · proteger a borda (WAF, rate limit) · observar (idade do outbox, lag do consumidor, DLT, 5xx, saturação dos pools) · custodiar segredos.

**O que não é.** Não é um ERP nem contabilidade completa (sem notas fiscais, impostos ou multimoeda) · não é um produto de interface — a entrega é API e documentação, e o frontend Blazor Server (task 0008, ADR-0010) é um cliente de demonstração sobre a mesma API · não provisiona AWS: o alvo é desenho, o que roda é local · não é migração de legado: a arquitetura de transição é um diferencial documentado · o consolidado é diário e eventualmente consistente, não tempo real · não é um sistema de produção — é um MVP que prova os mecanismos críticos.

**Restrições.** Os requisitos obrigatórios do desafio são piso: mapeamento de domínios e capacidades, refinamento de requisitos funcionais e não funcionais, arquitetura-alvo, justificativas, testes, README, repositório público e todas as documentações no repositório — se faltarem, a entrega é descartada. Prazo: **20/11/2026 pela manhã**. Stack: .NET. Execução local: docker compose (Postgres + Kafka). Os quatro diferenciais — transição, custos, observabilidade e segurança de integração — entram como documentação completa. O tempo é a restrição mais dura: o escopo cresce para documentação antes de crescer para código.

**Critério de sucesso.** Todos os obrigatórios atendidos; os quatro diferenciais documentados; os pontos difíceis provados localmente com testes — idempotência, estorno, entrega duplicada, DLT e consolidado; e cada decisão de arquitetura justificada com o trade-off explícito. A entrega deve permitir a quem lê concluir: este profissional sabe decompor, decidir e provar.

## Arquitetura de negócio

Blueprint local em `docs/ultron/agents/business/` — os domínios funcionais são as capacidades de nível 1 (CAP-1 a CAP-4).

capacidades: CAP-1.1, CAP-1.2, CAP-1.3, CAP-1.4, CAP-2.1, CAP-2.2, CAP-2.3, CAP-2.4, CAP-2.5, CAP-3.1, CAP-3.2, CAP-4.1, CAP-4.2
