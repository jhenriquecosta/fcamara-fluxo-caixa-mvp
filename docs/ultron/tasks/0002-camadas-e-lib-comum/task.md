# Task 0002-camadas-e-lib-comum — Separação em camadas e lib comum

tipo: refactor
branch: refactor/0002-camadas-e-lib-comum
publicacao: pendente
estado: concluído
responsavel: jhenriquecosta@hotmail.com
next_skill: 
issue: #10

## Contexto

O Operador avaliou a estrutura e pediu separar as responsabilidades e criar uma lib comum, usando o `Practical.CleanArchitecture` (Monolith/ModularMonolith) como referência: `CrossCuttingConcerns` independente, `Domain → CrossCuttingConcerns`, `Application → Domain`, `Infrastructure → Domain`, `WebAPI` como composition root, `Contracts` compartilhado e ArchTests.

## Escopo

- **Cashflow.Common**: auth (JWT, DevToken, políticas de escopo), Serilog, OpenTelemetry/Prometheus, request context (requestId/traceId/correlation), health e `/metrics`. Sem referência a domínio ou serviço.
- **Cashflow.Contracts**: o envelope do evento, os nomes de tópico e os tipos — compartilhados entre publicador e consumidor.
- **Lancamentos**: Domain (Lançamento, Tipo, regras do estorno, portas), Application (RegistrarLancamento, EstornarLancamento, DTOs), Infrastructure (Dapper/Npgsql, idempotência, outbox, Kafka, migrator), Api (composition root, endpoints).
- **Consolidado**: Domain (ConsolidadoDiario, a projeção), Application (ConsultarConsolidado, handler do evento), Infrastructure (consumidor Kafka, Dapper, migrator), Api.
- **Testes**: separar por tipo — unitários das regras, integração dos serviços, e2e do fluxo, ArchTests das dependências.
- README e o ADR da estrutura atualizados; compose e Dockerfiles com os novos caminhos.

## Critérios de aceitação

- [x] A suíte permanece verde (17/17 ou a reorganizada), incluindo o ArchTest das dependências.
- [x] O `docker compose up` sobe e o demo do README passa.
- [x] A `Domain` não referencia a `Infrastructure`; a `Api` é a única composition root; a lib comum não depende de serviço.
- [x] Nenhum comportamento observável muda: as respostas, os status, o evento e o consolidado continuam iguais.
