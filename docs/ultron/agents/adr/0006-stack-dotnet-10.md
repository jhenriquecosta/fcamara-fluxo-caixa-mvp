# 0006. Stack .NET 10 LTS e composição do MVP

| Campo | Conteúdo |
|---|---|
| **Documento** | ADR-0006 · Architecture Decision Record |
| **Projeto** | fcamara-fluxo-caixa-mvp |
| **Autor** | ULTRON · skill ultron-adr — Operador: J Henrique Costa |
| **Data** | 2026-11-18 |
| **Versão do documento** | 1.1 |
| **Status** | accepted |
| **Rastreabilidade** | Desafio Carrefour (via FCamara) · PRD-0001 · Research #0001 · ADR-010 (desenho-base) |

> **Uma linha:** O MVP é .NET 10 LTS com ASP.NET Core, Confluent.Kafka, Npgsql/Dapper e um outbox próprio — sem framework comercial e sem runtime fora de suporte.

## Contexto

A stack do Carrefour é .NET, e o desafio permite escolher a linguagem. A pesquisa mostra que **.NET 8 e .NET 9 chegaram ao fim do suporte em 10/11/2026**, oito dias antes desta decisão, e que o **.NET 10 LTS** é a versão vigente (suporte até 2028-11-14) (Research #0001, pergunta 1). O ADR-010 foi escrito com termos Java/JDBC; o desenho é agnóstico, mas a implementação precisa traduzir as notas.

A pesquisa também mudou uma escolha que parecia óbvia: o **MassTransit v9 virou comercial** (Massient, jan/2026) e exige chave de licença em todo processo; o v8 é Apache-2.0, mas o suporte termina em 2026. O **Wolverine é MIT**, com outbox nativo para EF Core/PostgreSQL (Research #0001, pergunta 4).

## Decisão

- **Runtime:** .NET 10 LTS (`net10.0`), ASP.NET Core 10; APIs organizadas por endpoint, com a autenticação em filtro.
- **Kafka:** Confluent.Kafka **2.16.0** (suporte a .NET 10).
- **PostgreSQL:** Npgsql **10.0.3** e Dapper **2.1.79** para SQL explícito; migrações SQL versionadas aplicadas na subida, com tabela de versão. O pool de conexões do Npgsql substitui as notas de JDBC do ADR-010.
- **Outbox e consumo:** implementação própria com Npgsql e `BackgroundService` — é o que o ADR-010 já desenha e mantém o mecanismo visível. **Wolverine** (MIT) fica como alternativa de framework; **MassTransit v9 não entra** por licença, e o v8 não entra por fim de suporte.
- **Autenticação:** Cognito no alvo (authorization code + PKCE para pessoas, client credentials para integradores, JWKS, escopos). No ambiente local, um emissor de desenvolvimento assina JWTs com as mesmas claims e escopos; a API valida issuer, audience e JWKS por configuração.
- **Testes:** xUnit v3, Testcontainers 4.x para PostgreSQL e Kafka, `WebApplicationFactory` para a API.
- **Local:** docker compose com PostgreSQL, Kafka 4.x em KRaft e os dois serviços; criação de tópico e migrações na subida.

## Alternativas consideradas

- **Java + Spring Boot** — era o que as notas de JDBC do ADR-010 sugeriam, mas a stack do cliente é .NET; rejeitado.
- **Node.js ou Go** — viáveis tecnicamente, mas fora da stack do cliente e sem ganho que justifique o desvio; rejeitado.
- **MassTransit v9** — rejeitado por licença comercial e chave por processo; **v8** — rejeitado por fim de suporte em 2026.
- **Wolverine** — alternativa legítima e MIT; não escolhido para o MVP porque o outbox próprio é menor, não esconde o mecanismo e basta para os dois dias.
- **EF Core em vez de Dapper** — produtivo para o CRUD, mas o outbox e a projeção pedem SQL explícito (`FOR UPDATE SKIP LOCKED`, upsert); Dapper deixa o desenho à vista. EF Core fica como alternativa caso o Wolverine seja adotado.

## Consequências

- **Positivas:** runtime com suporte até 2028; clientes Kafka e Postgres maduros e compatíveis com .NET 10; outbox visível e sem licença comercial; testes de integração com banco e broker reais.
- **Negativas / trade-offs aceitos:** o emissor de autenticação local é um substituto do Cognito; o .NET 11 (STS) chega em novembro/2026, mas não é LTS; o time precisa pinar as versões e acompanhar o suporte.
- **Desdobramentos:** confirmar a versão do PostgreSQL no RDS antes de provisionar; confirmar a região e os preços no Pricing Calculator; documentar no README como gerar o token local e como rodar os testes.

---

**Proveniência.** Desafio Carrefour — Requisitos e qualificações (stack .NET do Carrefour); ADR-010; PRD-0001; Research #0001, perguntas 1, 2, 4, 5 e 10; NuGet e documentação oficial dos mantenedores (consulta de 18/11/2026).

**Histórico de versões.**

| Versão | Data | Autor | Mudança |
|---|---|---|---|
| 1.0 | 2026-11-18 | ULTRON · ultron-adr | Criação |
| 1.1 | 2026-11-18 | ULTRON · ultron-adr | Operador aceita a decisão — status accepted |

**Rastreabilidade.** Desafio Carrefour (via FCamara) › repositório `fcamara-fluxo-caixa-mvp` › PRD-0001 › ADR-0006.

<sub>Gerado por ULTRON v2.1.1 · skill ultron-adr · 2026-11-18T20:27:21Z · em conformidade com a Convenção de Documento ULTRON. Documento versionado em git — a proveniência viaja com o projeto.</sub>
