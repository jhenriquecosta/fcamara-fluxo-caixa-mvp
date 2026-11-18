# ADR-0007 — Separação em camadas e lib comum

**Status:** superseded por [ADR-0009](0009-monolito-modular.md)
**Data:** 2026-11-18
**Decisores:** J Henrique Costa (Operador) · ULTRON
**Versão do documento:** 1.1
**Relacionados:** ADR-0001 (dois serviços), ADR-0006 (stack), `docs/arquitetura/arquitetura-alvo.md`

## Contexto

A entrega inicial concentrava, em cada serviço, um `Program.cs` com composição, autenticação, Serilog, OpenTelemetry, request context, health, `/metrics` e o dev token; a camada de aplicação misturava a regra de negócio, a idempotência e o SQL; o contrato do evento era um objeto anônimo com strings mágicas; e os testes de comportamentos distintos viviam num único projeto chamado `Lancamentos.Api.Tests`. O Operador pediu separar as responsabilidades e criar uma lib comum, usando o `Practical.CleanArchitecture` como referência — onde o `CrossCuttingConcerns` não depende de projeto, `Application → Domain`, `Infrastructure → Domain`, o `WebAPI` compõe, o `Contracts` é compartilhado e os ArchTests provam a direção.

## Decisão

- **`Cashflow.Common`** — a lib comum, sem dependência de serviço: auth (JWT, DevToken, políticas de escopo), Serilog, OpenTelemetry/Prometheus, request context (requestId/traceId/correlation), health, `/metrics` e o tempo (TimeProvider + fuso do negócio). O boilerplate compartilhado vive aqui: `AddCashflowPostgres`/`SqlSchema` (base de dados e migração), `CashflowDefaults` (configuração com precedência para o appsettings e o ambiente), `Hashes`, `CashflowProblems` (erros HTTP) e `KafkaOptions`/`CashflowKafka` (mensageria). O `CashflowJson`/`CashflowEventSerializer` ficam nos Contracts, donos do schema.
- **`Cashflow.Contracts`** — o envelope `CashflowEvent<T>`, o `LancamentoEventPayload`, os tipos de evento e os tópicos. É o schema do barramento, compartilhado pelos dois lados.
- **Por serviço, quatro camadas:**
  - `Domain` — as regras (`Lancamento`, `Estornar`, `DiaDeNegocio`, `LancamentoEvento.Delta`), as portas (`IRegistroDeLancamentos`, `IPortDeIdempotencia`, `IConsultaConsolidado`, `IProjecaoConsolidado`) e nenhuma dependência externa.
  - `Application` — os casos de uso (`RegistrarLancamentoHandler`, `EstornarLancamentoHandler`, `ConsultarConsolidadoHandler`, `ProjetarEventoHandler`), os comandos/resultados e a orquestração da idempotência.
  - `Infrastructure` — as implementações Dapper/Npgsql, o publicador e o consumidor Kafka, as métricas e o migrator.
  - `Api` — a composition root e os endpoints; referencia todas as camadas.
- **Direção das dependências:** `Domain ← Application ← Infrastructure`; o `Api` compõe tudo; o `Domain` não conhece nada fora de si.
- **Guard executável:** `tests/Cashflow.ArchTests` (NetArchTest) falha se a direção for violada.
- **Métricas na `Infrastructure`** (quem as emite); a `Api` as usa no mapeamento dos resultados.

**Fora de escopo agora, registrado:** MediatR/CQRS com dispatcher (a cerimônia não se paga em dois serviços; os handlers já separam os casos de uso), EF Core/Persistence (o Dapper é a escolha do ADR-0006), e `BackgroundServer`/`Migrator` como hosts separados (o ADR-0006 mantém o in-process; o alvo separa).

## Alternativas consideradas

- **Manter os dois projetos únicos** — duplicação de auth/logging/OTel, regra misturada com SQL, contrato sem versionamento. Rejeitada pelo Operador.
- **Adotar o template inteiro do `Practical.CleanArchitecture`** (MediatR, EF, Blazor, ArchTests, ContractTests) — a maior parte não serve ao tamanho do MVP. Adotada a estrutura de camadas e a lib comum, não o ferramental.
- **Lib comum só para auth/logging e manter as camadas no projeto da API** — resolveria a duplicação, mas não a separação de responsabilidades que o Operador pediu.

## Consequências

- Mudança de comportamento toca uma camada; trocar a persistência fica atrás das portas.
- A duplicação dos dois hosts some: auth, logging, OTel, request context, health e dev token num lugar só.
- Os ArchTests transformam a regra de dependência em teste determinístico.
- Custo: mais projetos e arquivos; o Dockerfile copia `src/` inteiro; o build/restore fica um pouco mais longo.
- O comportamento observável não muda — a suíte de 17 testes de integração continua verde.

## Histórico

| Versão | Data | Autor | Mudança |
|---|---|---|---|
| 1.0 | 2026-11-18 | ULTRON · ultron-adr | Estrutura em camadas, lib comum, contracts e arch tests; operador aprova a separação |
