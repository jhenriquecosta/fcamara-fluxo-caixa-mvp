# Fluxo de caixa — MVP (desafio Carrefour / FCamara)

Dois serviços em .NET 10 — **Lançamentos** e **Consolidado** — que registram débitos e créditos com idempotência, corrigem por contra-lançamento e derivam o saldo diário por evento. A arquitetura-alvo é AWS (desenhada no ADR-010); o que roda aqui é o MVP local, com PostgreSQL, Kafka 4 em KRaft, ELK e Prometheus/Grafana, e um frontend em Blazor Server com login e perfis.

## O problema

Um comerciante precisa controlar o fluxo de caixa diário e consultar o saldo consolidado. O serviço de lançamentos não pode parar quando o consolidado cai; em pico, o consolidado recebe 50 requisições por segundo com no máximo 5% de perda. A solução registra o movimento uma única vez, corrige sem apagar o histórico e deriva o saldo sem que a falha de um serviço derrube o outro.

## Arquitetura

- **Lançamentos** grava o lançamento, a chave de idempotência e o evento de outbox na mesma transação, e publica no Kafka.
- **Consolidado** consome o evento, ignora repetidos e mantém a projeção `consolidado_diario` (`saldoDia`, `saldoAnterior`, `saldoAcumulado`).
- O estorno é um **novo lançamento** de tipo oposto; o original nunca é editado.
- O código é um **monólito modular** no molde do [`ModularMonolith`](https://github.com/jhenriquecosta/Practical.CleanArchitecture/tree/master/src/ModularMonolith) do Practical.CleanArchitecture: projetos compartilhados (CrossCuttingConcerns, Domain, Application, Contracts, Infrastructure), um módulo por serviço (`Cashflow.Modules.Lancamentos`, `Cashflow.Modules.Consolidado`) e três hosts (`WebAPI`, `BackgroundServer`, `Migrator`). Cada caso de uso é um `ICommand`/`IQuery` com o handler, acionado pelo `Dispatcher`.
- O mesmo `Cashflow.WebAPI` sobe **duas vezes** no deploy, cada uma com um módulo ligado (`Modules:<X>:Enabled`). Por isso uma queda do consolidado não derruba os lançamentos. O outbox e o consumidor rodam no `BackgroundServer`.
- O **frontend** (`Cashflow.Blazor.Modules` + `Cashflow.BlazorServerSide`, no molde do `ClassifiedAds.Blazor.Modules`/`BlazorServerSide` da referência) é mais um cliente da API: só fala HTTP, com um endereço por serviço. O login cria uma sessão por cookie `HttpOnly`; o host emite, por chamada, um token de 5 minutos com só os escopos do perfil, e o token nunca chega ao navegador (Blazor Server como BFF, ADR-0010). Perfis: **operador** (lança, estorna, consulta) e **consulta** (só lê); quem garante é a API, por escopo.
- O desenho-base está em [`docs/requisitos/Arquietura-solucao-v2.png`](docs/requisitos/Arquietura-solucao-v2.png); o texto com os diagramas C4 e o mapeamento local ↔ AWS está em [`docs/arquitetura/arquitetura-alvo.md`](docs/arquitetura/arquitetura-alvo.md).

Decisões registradas em [ADRs](docs/ultron/agents/adr/): estilo event-driven com outbox (0001), Kafka em KRaft (0002), PostgreSQL com bancos lógicos (0003), idempotência e estorno (0004), projeção e dia do registro (0005), stack .NET 10 (0006), a avaliação de event sourcing contra o ledger com outbox (0008) o monólito modular no molde da referência (0009, que substitui o 0007) e o frontend Blazor Server como BFF (0010). Os requisitos estão no [PRD](docs/requisitos/requisitos.md).

## Como rodar localmente

Pré-requisitos: Docker com Compose (para o MVP), .NET 10 SDK (para os testes) e `jq` (para o roteiro abaixo).

```bash
docker compose up -d --build
observability/scripts/apply-elk.sh   # template de índice, ILM e data view do Kibana
```

> Se a stack já existia de uma execução anterior, rode `docker compose down -v` antes: o init do PostgreSQL cria os usuários por serviço só na primeira subida do volume.


| Serviço | URL |
|---|---|
| **Frontend** (`web`, Blazor Server) | http://localhost:8090 |
| Lançamentos (`lancamentos-api`, WebAPI só com Lançamentos) | http://localhost:8080 |
| Consolidado (`consolidado-api`, WebAPI só com Consolidado) | http://localhost:8081 |
| Background server (outbox e consumidor: `/health`, `/metrics`) | http://localhost:8082 |
| Kibana (logs) | http://localhost:5601 |
| Grafana (métricas) | http://localhost:3000 (admin/admin) |
| Prometheus | http://localhost:9090 |
| Elasticsearch | http://localhost:9200 |

O primeiro `up` baixa as imagens e compila os hosts. O `migrator` aplica o esquema de cada módulo e sai, e só então os hosts sobem.

### Pela tela

Abra http://localhost:8090. Usuários de demonstração (só no ambiente `Development`; as senhas estão em hash PBKDF2 no `appsettings.Development.json`):

| Usuário | Senha | Perfil | Comerciante |
|---|---|---|---|
| `operador` | `Operador@123` | lança, estorna, consulta | loja 1 |
| `consulta` | `Consulta@123` | só consulta | loja 1 |
| `loja2` | `Loja2@123` | lança, estorna, consulta | loja 2 (não vê a loja 1) |

Lance um crédito e um débito em **Novo lançamento**, veja-os em **Lançamentos** e o saldo em **Consolidado**: ele reflete o evento em segundos (projeção eventualmente consistente). Estorne um lançamento: nasce o lançamento oposto, o original fica riscado no histórico, e o consolidado volta. Entre como `consulta`: o menu não mostra lançar nem estornar, e `/lancamentos/novo` responde acesso negado. Pare o `consolidado-api` (`docker compose stop consolidado-api`): a tela de lançamentos continua funcionando.

### Roteiro do demo pela API

```bash
# 1. token de desenvolvimento (Cognito é o alvo; aqui o emissor é local)
TOKEN=$(curl -s -X POST localhost:8080/dev/token -H 'Content-Type: application/json' \
  -d '{"comercianteId":"11111111-1111-1111-1111-111111111111","scopes":["lancamentos.write","consolidado.read"]}' \
  | jq -r .token)

# 2. um crédito de 100
LANCAMENTO=$(curl -s -X POST localhost:8080/api/v1/lancamentos \
  -H "Authorization: Bearer $TOKEN" -H 'Idempotency-Key: demo-1' -H 'Content-Type: application/json' \
  -d '{"valor":100.00,"tipo":"credito","descricao":"venda"}')
echo "$LANCAMENTO"

# 3. o mesmo envio não duplica
curl -s -X POST localhost:8080/api/v1/lancamentos \
  -H "Authorization: Bearer $TOKEN" -H 'Idempotency-Key: demo-1' -H 'Content-Type: application/json' \
  -d '{"valor":100.00,"tipo":"credito","descricao":"venda"}'

# 4. o consolidado do dia (o dia é o do registro, America/Sao_Paulo)
DIA=$(TZ=America/Sao_Paulo date +%F)
curl -s "localhost:8081/api/v1/consolidado/$DIA" -H "Authorization: Bearer $TOKEN"

# 5. estorno do lançamento (contra-lançamento com nova chave)
ID=$(echo "$LANCAMENTO" | jq -r .id)
curl -s -X POST "localhost:8080/api/v1/lancamentos/$ID/estornos" \
  -H "Authorization: Bearer $TOKEN" -H 'Idempotency-Key: demo-estorno-1'

# 6. o consolidado do dia volta a zero
curl -s "localhost:8081/api/v1/consolidado/$DIA" -H "Authorization: Bearer $TOKEN"
```

No Kibana, a data view `cashflow-logs-*` mostra os logs JSON dos dois serviços. No Grafana, o dashboard **Fluxo de caixa — Golden Signals** mostra tráfego, erros, latência, idade do outbox, lag, DLT e lançamentos por resultado. As regras do Prometheus incluem os alarmes desses sinais.

## Testes

```bash
dotnet test --solution Cashflow.slnx
```

A suíte tem 91 testes, organizados como os da referência. Os de integração e o ponta a ponta sobem PostgreSQL e Kafka reais via Testcontainers:

| Projeto | Testes | O que cobre |
|---|---|---|
| `Modules.Lancamentos.UnitTests` | 13 | regras do lançamento e do estorno, dia do negócio em America/Sao_Paulo, tipo e hash do corpo |
| `Modules.Lancamentos.IntegrationTests` | 17 | idempotência (replay, 422 de payload, 409 de concorrência), estorno, listagem do dia com a marca de estornado, auth, métricas, o WebAPI sem trabalho de fundo e com só o módulo ligado |
| `Modules.Consolidado.UnitTests` | 4 | delta do movimento, saldo anterior, dia sem movimento |
| `Modules.Consolidado.IntegrationTests` | 4 | consulta, data inválida, módulo isolado, carga de 50 req/s com ≥95% de sucesso |
| `EndToEndTests` | 5 | a topologia do compose (WebAPI duas vezes e BackgroundServer): fluxo até o consolidado, estorno, entrega duplicada somando uma vez, DLT após 1+3 tentativas, lançamentos de pé com o consolidado fora |
| `BlazorServerSide.IntegrationTests` | 28 | login (redirect sem sessão, credencial inválida, open redirect, cookie `HttpOnly`/`SameSite`), perfis, logout com antiforgery, token ausente do HTML e do cookie, escopos do token por perfil, e as telas contra a API real: lançar, listar, estornar, consolidado, 403 da API para o perfil consulta |
| `ArchTests` | 20 | a direção das dependências e o mecanismo: todo comando ou consulta tem handler, e endpoint, worker e consumer passam pelo `Dispatcher`; o frontend só conhece o contrato HTTP |

## Observabilidade

A convenção de logs e as métricas estão em [`docs/ultron/agents/logging.md`](docs/ultron/agents/logging.md); o stack local e o mapeamento para o alvo estão em [`observability/README.md`](observability/README.md). A aplicação emite OpenTelemetry e JSON — trocar ELK/Prometheus por CloudWatch é configuração, não código. Os SLOs propostos estão em [`observability/slos/cashflow.yml`](observability/slos/cashflow.yml).

## Diferenciais do desafio

- **Arquitetura-alvo e C4:** [`docs/arquitetura/arquitetura-alvo.md`](docs/arquitetura/arquitetura-alvo.md)
- **Arquitetura de transição:** [`docs/arquitetura/transicao.md`](docs/arquitetura/transicao.md)
- **Estimativa de custos:** [`docs/arquitetura/custos.md`](docs/arquitetura/custos.md)
- **Observabilidade:** [`docs/arquitetura/observabilidade.md`](docs/arquitetura/observabilidade.md)
- **Segurança para consumo dos serviços:** [`docs/arquitetura/seguranca-integracao.md`](docs/arquitetura/seguranca-integracao.md)

## Estrutura

```
src/Cashflow.CrossCuttingConcerns/      IDateTimeProvider, exceções, extensões, hashes (não depende de nada)
src/Cashflow.Domain/                    Entity, IAggregateRoot, eventos de domínio, IRepository/IUnitOfWork, IMessageBus e o MessageBus
src/Cashflow.Application/               ICommand/IQuery e handlers, Dispatcher, HandlerFactory, decorators [AuditLog] e [DatabaseRetry]
src/Cashflow.Contracts/                 envelope do evento, payload, tipos e tópicos (schema do barramento, sem dependência)
src/Cashflow.Infrastructure/            Kafka (sender, receiver com retry e DLT), consumer hospedado, DbContext Dapper, Serilog, OTel, auth
src/Cashflow.Modules.Lancamentos/       Commands, Entities, EventHandlers (outbox), OutBoxEventPublishers, HostedServices, Repositories, Endpoints
src/Cashflow.Modules.Consolidado/       Queries, Commands (projeção), MessageBusConsumers, Entities, Repositories, Endpoints
src/Cashflow.WebAPI/                    host HTTP; liga os módulos por Modules:<X>:Enabled
src/Cashflow.BackgroundServer/          host dos trabalhos de fundo: publicador do outbox e consumidor
src/Cashflow.Migrator/                  aplica o esquema de cada módulo no banco dele
src/Cashflow.Blazor.Modules/            telas e clientes HTTP por módulo (Core, Lancamentos, Consolidado); só conhece o contrato HTTP
src/Cashflow.BlazorServerSide/          host do frontend: login, perfis, emissão do token de acesso no servidor
tests/Cashflow.Modules.*.UnitTests/     regras dos módulos, sem infraestrutura
tests/Cashflow.Modules.*.IntegrationTests/  o módulo pelo WebAPI, com PostgreSQL real
tests/Cashflow.EndToEndTests/           a topologia do compose, com PostgreSQL e Kafka reais
tests/Cashflow.BlazorServerSide.IntegrationTests/  o frontend pelo HTTP do navegador e contra a API real
tests/Cashflow.ArchTests/               direção das dependências e o mecanismo comando/consulta (NetArchTest)
tests/Cashflow.Testing/                 fixtures e fábricas dos hosts para os testes
observability/                          ELK, Prometheus, Grafana, SLOs
infra/postgres/                         criação do banco lógico do consolidado e dos usuários por serviço
docs/requisitos/                        PRD e desenho-base
docs/ultron/agents/                     visão, negócio, ADRs, logging, research
compose.yaml                            PostgreSQL, Kafka, migrator, lancamentos-api, consolidado-api, background-server, web, ELK, Prometheus, Grafana
```

## Fora de escopo

Cadastro de usuários e comerciantes (os de demo são configurados); provisionamento AWS (o alvo é desenho); ERP/contabilidade completa; consolidação em tempo real; migração de legado em código (a transição é documentada); lançamento retroativo (o dia é o do registro); multi-região.

## Notas

- A autenticação local tem dois emissores: o `/dev/token` (só em Development, para o roteiro por `curl`) e o login do frontend, que emite tokens de 5 minutos com o escopo do perfil (ADR-0010). O alvo usa Cognito com authorization code + PKCE e client credentials, e os segredos ficam no Secrets Manager.
- O MVP usa uma instância PostgreSQL com dois bancos lógicos; a separação física é a evolução registrada no ADR-0003.
- Entrega do desafio técnico para a vaga de Arquiteto de Soluções do Carrefour, via FCamara.
