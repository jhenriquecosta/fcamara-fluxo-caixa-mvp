# Fluxo de caixa — MVP (desafio Carrefour / FCamara)

Dois serviços em .NET 10 — **Lançamentos** e **Consolidado** — que registram débitos e créditos com idempotência, corrigem por contra-lançamento e derivam o saldo diário por evento. A arquitetura-alvo é AWS (desenhada no ADR-010); o que roda aqui é o MVP local, com PostgreSQL, Kafka 4 em KRaft, ELK e Prometheus/Grafana, e um frontend em Blazor Server com login e perfis.

## O problema

Um comerciante precisa controlar o fluxo de caixa diário e consultar o saldo consolidado. O serviço de lançamentos não pode parar quando o consolidado cai; em pico, o consolidado recebe 50 requisições por segundo com no máximo 5% de perda. A solução registra o movimento uma única vez, corrige sem apagar o histórico e deriva o saldo sem que a falha de um serviço derrube o outro.

## Arquitetura

- **Lançamentos** grava o lançamento, a chave de idempotência e o evento de outbox na mesma transação, e publica no Kafka.
- **Consolidado** consome o evento, ignora repetidos e mantém a projeção `consolidado_diario` (`saldoDia`, `saldoAnterior`, `saldoAcumulado`).
- O estorno é um **novo lançamento** de tipo oposto; o original nunca é editado.
- O código é um **monólito modular**: projetos compartilhados (CrossCuttingConcerns, Domain, Application, Contracts, Infrastructure), um módulo por serviço (`Cashflow.Modules.Lancamentos`, `Cashflow.Modules.Consolidado`) e três hosts (`WebAPI`, `BackgroundServer`, `Migrator`). A escolha tem motivo por eixo: a **fronteira de cada serviço** fica explícita no código e é verificada por teste (ArchTests), e o **mesmo binário sobe duas vezes**, cada um com o módulo que serve — o isolamento de falha mora no deploy, sem código duplicado. Cada caso de uso é um `ICommand`/`IQuery` com handler, acionado pelo `Dispatcher`, que é onde autorização, auditoria e nova tentativa de banco entram como decorators em vez de se repetirem em cada handler.
- O mesmo `Cashflow.WebAPI` sobe **duas vezes** no deploy, cada uma com um módulo ligado (`Modules:<X>:Enabled`). Por isso uma queda do consolidado não derruba os lançamentos. O outbox e o consumidor rodam no `BackgroundServer`.
- A **configuração tipada com Options** e o **Strategy nos pontos de variação** não entraram no MVP: estão avaliados com evidência no [ADR-0012](docs/ultron/agents/adr/0012-options-validados-e-estrategias-nos-pontos-de-variacao.md) e no [documento de avaliação](docs/arquitetura/avaliacao-de-padroes.md), com a ordem e as dependências das tasks que os adotam — a justificativa de cada um está lá, não presumida aqui.
- O **frontend** (`Cashflow.Blazor.Modules` + `Cashflow.BlazorServerSide`) é mais um cliente da API: só fala HTTP, com um endereço por serviço. O login cria uma sessão por cookie `HttpOnly`; o host emite, por chamada, um token de 5 minutos com só os escopos do perfil, e o token nunca chega ao navegador (Blazor Server como BFF, ADR-0010). Perfis: **operador** (lança, estorna, consulta) e **consulta** (só lê); quem garante é a API, por escopo. A validação do JWT tem duas estratégias escolhidas por configuração (ADR-0012): **`Auth:Authority`** liga a validação pelos metadados OIDC e pelo **JWKS** do emissor — o caminho do Cognito, provado por teste com um emissor OIDC falso — e, sem ela, o local usa a chave simétrica de desenvolvimento, e só nesse modo o `/dev/token` existe.
- O desenho-base está em [`docs/requisitos/Arquietura-solucao-v2.png`](docs/requisitos/Arquietura-solucao-v2.png); o texto com os diagramas C4 e o mapeamento local ↔ AWS está em [`docs/arquitetura/arquitetura-alvo.md`](docs/arquitetura/arquitetura-alvo.md).

Decisões registradas em [ADRs](docs/ultron/agents/adr/): estilo event-driven com outbox (0001), Kafka em KRaft (0002), PostgreSQL com bancos lógicos (0003), idempotência e estorno (0004), projeção e dia do registro (0005), stack .NET 10 (0006), a avaliação de event sourcing contra o ledger com outbox (0008), o monólito modular (0009, que substitui o 0007), o frontend Blazor Server como BFF (0010), a superfície de demonstração — um cliente, duas UIs e o `start.sh` (0011), a avaliação de padrões com Options e Strategy nos pontos de variação (0012, proposta) e a superfície OpenAPI nativa com escopos por rota (0013). Os requisitos estão no [PRD](docs/requisitos/requisitos.md).

## Como rodar localmente

Pré-requisitos: Docker com Compose (para o MVP), .NET 10 SDK (para os testes) e `jq` (para o roteiro abaixo).

### O caminho curto: `./start.sh`

Um comando faz o ciclo inteiro — pré-requisitos, suíte, stack, saúde, ELK e o painel de acesso:

```bash
./start.sh               # up: pré-requisitos → suíte → stack → saúde → ELK → painel (--sem-testes pula a suíte)
./start.sh demo          # o roteiro narrado: lançar → outbox → Kafka → consolidado → estorno → idempotência
./start.sh terminal      # a UI de terminal (menu por perfil; sobe a stack se preciso)
./start.sh test          # a suíte, com cada teste aparecendo ao passar
./start.sh load          # a prova de carga do RNF-2 (k6 no container oficial)
./start.sh status        # o estado da stack e a saúde de cada serviço
./start.sh logs web      # os logs de um serviço (sem argumento: todos)
./start.sh down          # derruba a stack (volumes só com --volumes)
```

O passo a passo manual continua abaixo, para quem quiser cada etapa à vista.

### Prova de carga (RNF-2)

O requisito de pico do enunciado é 50 requisições por segundo no consolidado com no máximo 5% de perda. `./start.sh load` sobe a stack se preciso e roda o roteiro do k6 (`load/k6-consolidado.js`) no container oficial, com a mesma chegada constante de tráfego e os limites no próprio script:

```text
cenário: 50 iterações/s por 60 s · 3.000 requisições
sucesso: 100% (0 falhas) · p95 = 2,49 ms · limites p95 < 200 ms e falha < 5% ✓
medido em 19/11/2026, stack local (Docker no host, PostgreSQL e consolidado-api do compose)
```

O p95 configurado no limite é o RNF-3 (proposta) para `GET /consolidado` — a mesma execução cobre os dois.

### O passo a passo manual

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

### Contrato OpenAPI

Cada API publica o próprio contrato em `/openapi/v1.json`, sempre e sem autenticação — cada documento lista só as rotas do módulo daquele deploy:

- Lançamentos: http://localhost:8080/openapi/v1.json
- Consolidado: http://localhost:8081/openapi/v1.json

Em Development, a UI Scalar renderiza esse documento em http://localhost:8080/scalar e http://localhost:8081/scalar. A decisão está na [ADR-0013](docs/ultron/agents/adr/0013-superficie-openapi.md).

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

A suíte tem **147 testes**, um por regra que prova. Os de integração e o ponta a ponta sobem PostgreSQL e Kafka reais via Testcontainers. O nome de cada teste é a regra que ele prova, e o critério de aceite do PRD vai num `Trait`: `dotnet test --solution Cashflow.slnx --filter-trait "Criterio=AC4"` roda só o que prova o AC4.

| Projeto | Testes | O que cobre |
|---|---|---|
| `Modules.Lancamentos.UnitTests` | 24 | regras do lançamento e do estorno, dia do negócio em America/Sao_Paulo, tipo do contrato e impressão digital do corpo |
| `Modules.Lancamentos.IntegrationTests` | 34 | idempotência (replay, 422 de payload, 409 de concorrência), estorno (inclusive de outro comerciante), listagem do dia com a marca de estornado, auth, métricas, o WebAPI sem trabalho de fundo e com só o módulo ligado, o documento OpenAPI com escopos por rota e a saúde live/ready |
| `Modules.Consolidado.UnitTests` | 5 | efeito no saldo, estorno anulando o original, saldo de abertura, dia sem movimento |
| `Modules.Consolidado.IntegrationTests` | 6 | consulta, data inválida, módulo isolado, carga de 50 req/s com ≥95% de sucesso, e o documento OpenAPI do deploy |
| `EndToEndTests` | 5 | a topologia do compose (WebAPI duas vezes e BackgroundServer): fluxo até o consolidado, estorno, entrega duplicada somando uma vez, DLT após 1+3 tentativas, lançamentos de pé com o consolidado fora |
| `BlazorServerSide.IntegrationTests` | 33 | login (redirect sem sessão, credencial inválida, open redirect, cookie `HttpOnly`/`SameSite`), perfis, logout com antiforgery, token ausente do HTML e do cookie, escopos do token por perfil, as telas contra a API real, o painel de infraestrutura e a saúde live/ready |
| `Terminal.Tests` | 19 | menu por perfil, lançar, listar e estornar, consolidado, telas de infraestrutura, demo e o token de desenvolvimento — com `TestConsole` e API falsa |
| `ArchTests` | 21 | a direção das dependências e o mecanismo: todo comando ou consulta tem handler, endpoint, worker e consumer passam pelo `Dispatcher`; frontend e terminal só conhecem o contrato HTTP |

## Observabilidade

A convenção de logs e as métricas estão em [`docs/ultron/agents/logging.md`](docs/ultron/agents/logging.md); o stack local e o mapeamento para o alvo estão em [`observability/README.md`](observability/README.md). A aplicação emite OpenTelemetry e JSON — trocar ELK/Prometheus por CloudWatch é configuração, não código. Os SLOs propostos estão em [`observability/slos/cashflow.yml`](observability/slos/cashflow.yml).

## Diferenciais do desafio

- **Arquitetura-alvo e C4:** [`docs/arquitetura/arquitetura-alvo.md`](docs/arquitetura/arquitetura-alvo.md)
- **Arquitetura de transição:** [`docs/arquitetura/transicao.md`](docs/arquitetura/transicao.md)
- **Estimativa de custos:** [`docs/arquitetura/custos.md`](docs/arquitetura/custos.md)
- **Observabilidade:** [`docs/arquitetura/observabilidade.md`](docs/arquitetura/observabilidade.md)
- **Segurança para consumo dos serviços:** [`docs/arquitetura/seguranca-integracao.md`](docs/arquitetura/seguranca-integracao.md)
- **Avaliação de padrões (Options e Strategy) e dívida técnica:** [`docs/arquitetura/avaliacao-de-padroes.md`](docs/arquitetura/avaliacao-de-padroes.md)

## Estrutura

Cada projeto tem o seu `README.md` — por que existe, a finalidade, os padrões e o estilo arquitetural a que adere, e o que pode e não pode referenciar. Todo tipo e membro público de `src/` tem documentação XML, e o compilador cobra (`src/Directory.Build.props`).

| Projeto | Papel | README |
|---|---|---|
| `Cashflow.CrossCuttingConcerns` | IDateTimeProvider, pseudônimos de log, exceções e extensões (não depende de nada) | [README](src/Cashflow.CrossCuttingConcerns/README.md) |
| `Cashflow.Domain` | Entity, IAggregateRoot, eventos de domínio, IRepository/IUnitOfWork, IMessageBus | [README](src/Cashflow.Domain/README.md) |
| `Cashflow.Application` | ICommand/IQuery, handlers, Dispatcher, decorators `[AuditLog]` e `[DatabaseRetry]` | [README](src/Cashflow.Application/README.md) |
| `Cashflow.Contracts` | envelope do evento, payload, tipos e tópicos (schema do barramento) | [README](src/Cashflow.Contracts/README.md) |
| `Cashflow.Infrastructure` | Kafka com retry e DLT, consumer hospedado, Dapper, Serilog, OTel, auth | [README](src/Cashflow.Infrastructure/README.md) |
| `Cashflow.Modules.Lancamentos` | Commands, Entities, EventHandlers (outbox), publicadores, repositórios e endpoints | [README](src/Cashflow.Modules.Lancamentos/README.md) |
| `Cashflow.Modules.Consolidado` | Queries, Commands (projeção), consumidores, Entities, repositórios e endpoints | [README](src/Cashflow.Modules.Consolidado/README.md) |
| `Cashflow.WebAPI` | host HTTP; liga os módulos por `Modules:<X>:Enabled` | [README](src/Cashflow.WebAPI/README.md) |
| `Cashflow.BackgroundServer` | host dos trabalhos de fundo: publicador do outbox e consumidor | [README](src/Cashflow.BackgroundServer/README.md) |
| `Cashflow.Migrator` | aplica o esquema de cada módulo no banco dele | [README](src/Cashflow.Migrator/README.md) |
| `Cashflow.Blazor.Modules` | telas e clientes HTTP por módulo; só conhece o contrato HTTP | [README](src/Cashflow.Blazor.Modules/README.md) |
| `Cashflow.BlazorServerSide` | host do frontend: login, perfis, emissão do token no servidor | [README](src/Cashflow.BlazorServerSide/README.md) |
| `Cashflow.Terminal` | a segunda UI: menu por perfil, demo narrada e telas de infraestrutura | [README](src/Cashflow.Terminal/README.md) |
| `Cashflow.Modules.*.UnitTests` | regras dos módulos, sem infraestrutura | [Lancamentos](tests/Cashflow.Modules.Lancamentos.UnitTests/README.md) · [Consolidado](tests/Cashflow.Modules.Consolidado.UnitTests/README.md) |
| `Cashflow.Modules.*.IntegrationTests` | o módulo pelo WebAPI, com PostgreSQL real | [Lancamentos](tests/Cashflow.Modules.Lancamentos.IntegrationTests/README.md) · [Consolidado](tests/Cashflow.Modules.Consolidado.IntegrationTests/README.md) |
| `Cashflow.EndToEndTests` | a topologia do compose, com PostgreSQL e Kafka reais | [README](tests/Cashflow.EndToEndTests/README.md) |
| `Cashflow.BlazorServerSide.IntegrationTests` | o frontend pelo HTTP do navegador e contra a API real | [README](tests/Cashflow.BlazorServerSide.IntegrationTests/README.md) |
| `Cashflow.Terminal.Tests` | a UI de terminal com `TestConsole` e API falsa | [README](tests/Cashflow.Terminal.Tests/README.md) |
| `Cashflow.ArchTests` | direção das dependências e o mecanismo comando/consulta (NetArchTest) | [README](tests/Cashflow.ArchTests/README.md) |
| `Cashflow.Testing` | fixtures e fábricas dos hosts para os testes | [README](tests/Cashflow.Testing/README.md) |
| `observability/` | ELK, Prometheus, Grafana e SLOs | [README](observability/README.md) |

Fora dos projetos: `infra/postgres/` cria o banco lógico do consolidado e os usuários por serviço; `docs/requisitos/` guarda o PRD e o desenho-base; `docs/ultron/agents/` reúne visão, negócio, ADRs, convenção de log e research; e `compose.yaml` declara PostgreSQL, Kafka, migrator, lancamentos-api, consolidado-api, background-server, web, ELK, Prometheus e Grafana.

## Fora de escopo

Cadastro de usuários e comerciantes (os de demo são configurados); provisionamento AWS (o alvo é desenho); ERP/contabilidade completa; consolidação em tempo real; migração de legado em código (a transição é documentada); lançamento retroativo (o dia é o do registro); multi-região.

## Notas

- A autenticação local tem dois emissores: o `/dev/token` (só em Development, para o roteiro por `curl`) e o login do frontend, que emite tokens de 5 minutos com o escopo do perfil (ADR-0010). O alvo usa Cognito com authorization code + PKCE e client credentials, e os segredos ficam no Secrets Manager.
- O MVP usa uma instância PostgreSQL com dois bancos lógicos; a separação física é a evolução registrada no ADR-0003.
- Entrega do desafio técnico para a vaga de Arquiteto de Soluções do Carrefour, via FCamara.
