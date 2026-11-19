# Research #0004 — OpenAPI nativo no .NET 10: transformers, XML comments, Scalar e ProblemDetails

**Data:** 2026-11-19
**Contexto:** task 0010-openapi-das-apis (issue #26) · questões abertas R1–R4 da `descoberta.md`
**Context7:** disponível (autenticado durante a pesquisa) — doc versionada de `/dotnet/aspnetcore.docs` e `/scalar/scalar`; páginas oficiais do Microsoft Learn e do Scalar via web

---

## Perguntas investigadas

### 1. (R1) Como declarar o security scheme e os requisitos por operação no `Microsoft.AspNetCore.OpenApi` 10.0.x

**Resposta:** O pacote expõe `OpenApiOptions.AddDocumentTransformer` e `AddOperationTransformer` (delegate, instância ou tipo via DI), com as interfaces `IOpenApiDocumentTransformer` e `IOpenApiOperationTransformer`. O **scheme** é registrado uma vez, num document transformer (`document.Components.SecuritySchemes`); o **requisito** é adicionado por operação, num operation transformer, que lê a metadata do endpoint por `context.Description.ActionDescriptor.EndpointMetadata` — o mesmo caminho do exemplo oficial que filtra `AllowAnonymousAttribute`, e o que permite ler `IAuthorizeData` para obter a política (= escopo).

**Achado que muda o desenho:** com `SecuritySchemeType.Http` (Bearer), o array de escopos do requisito é **sempre vazio** nos exemplos oficiais — e a especificação OpenAPI só carrega escopos formalmente em esquemas `OAuth2` e `OpenIdConnect`. Para o documento **declarar os escopos por rota** (critério da task), o scheme precisa ser OAuth2 (client credentials) ou OpenIdConnect; o Bearer HTTP declararia os escopos só como prosa na descrição.

**Fontes:**
- Microsoft Learn — *Customize OpenAPI documents* (aspnetcore-10.0), ms.date 2026-10-21, acesso 2026-11-19 — primária
- Context7 `/dotnet/aspnetcore.docs` — `customize-openapi.md` (exemplo `AuthOperationTransformer`), acesso 2026-11-19 — primária

**Confiança:** alta para a mecânica dos transformers e da metadata; alta para a limitação de escopos formais (especificação + exemplos oficiais).

### 2. (R2) Os comentários XML chegam ao documento no .NET 10, inclusive para handlers de minimal API?

**Resposta:** Sim — a integração é automática quando o projeto gera o arquivo de documentação (`<GenerateDocumentationFile>true</GenerateDocumentationFile>`), o que o `Directory.Build.props` de `src/` já faz para todos os projetos. O pacote resolve os comentários da **assembly da aplicação e das assemblies referenciadas** com a propriedade ligada, estaticamente em tempo de compilação (source generator `XmlCommentGenerator`), e **não exige configuração no `AddOpenApi`**. Tags suportadas: `<summary>`, `<remarks>`, `<param>`, `<returns>`, `<response>`, `<example>`, `<deprecated>` e `<inheritdoc>`. **Limitações documentadas:** o nome do documento precisa ser literal (`AddOpenApi()`/`AddOpenApi("v1")`; nome vindo de variável desliga o suporte); `<inheritdoc>` resolve só no assembly em compilação; e `<response>` dá a descrição de um status, mas **não declara** o status — a declaração precisa vir de `Produces`/`TypedResults`. Não há **nenhuma** restrição documentada de visibilidade ou forma do handler; o exemplo oficial é um método `public static IResult` mapeado por método. Para o que os XML docs não cobrirem, `WithSummary`/`[EndpointSummary]` e `WithDescription` existem, mas a precedência entre eles e os comentários não é documentada.

**Ponto residual:** as páginas não afirmam nada sobre handlers `private static` (o único exemplo é `public static`), e o generator não documenta filtro de visibilidade. O teste de integração do documento responde isso na prática; se não associar, o documento sai com esquema e sem descrição — o que a descoberta (D5) já aceitou.

**Fontes:**
- Microsoft Learn — *Include metadata in the OpenAPI document* (aspnetcore-10.0), ms.date 2026-10-22, acesso 2026-11-19 — primária
- Microsoft Learn — *OpenAPI XML documentation comment support* (aspnetcore-10.0), ms.date 2026-08-19, acesso 2026-11-19 — primária
- Context7 `/dotnet/aspnetcore.docs` — `openapi-comments.md` (tags suportadas, projeto referenciado, limitação do nome literal), acesso 2026-11-19 — primária

**Confiança:** alta para o mecanismo, as tags e as limitações; média para o caso específico de handler privado.

### 3. (R3) `Scalar.AspNetCore` 2.16.16 com .NET 10: registro, rota e Bearer

**Resposta:** `app.MapScalarApiReference()` publica a UI em `/scalar` por padrão; `options.AddDocument("v1")` resolve para o padrão global `/openapi/{documentName}.json`, exatamente o caminho de `MapOpenApi`. A UI **só mostra autenticação se o documento declarar o scheme**; para pré-selecionar e pré-preencher o Bearer há `AddPreferredSecuritySchemes("...")` + `AddHttpAuthentication(...)` (e `WithPersistentAuthentication()` para persistir — os docs do GitHub e do site divergem no nome, e a 2.16.16 não tem entrada no changelog que confirme qual vale; a 2.16.16 é a versão em cache). Pré-preencher credencial é visível no navegador — a própria doc avisa que não é para produção. O mapeamento condicional por ambiente é responsabilidade do app (a doc oficial põe `MapScalarApiReference` dentro de `if (IsDevelopment())`).

**Versões:** 2.16.16 publicada em 2026-07-20, TFMs `net8.0`/`net9.0`/`net10.0`, sem dependências. A atual é 3.0.2 (2026-11-18), que abandonou `net8.0`/`net9.0` e tem um dia de vida; a última 2.x é 2.21.0. **Recomendação:** fixar 2.16.16 (em cache, suporte a .NET 10 confirmado, sem surpresa de lançamento recente).

**Fontes:**
- Context7 `/scalar/scalar` — `multiple-openapi-documents.md`, `authentication.md`, acesso 2026-11-19 — primária
- `scalar.com/products/api-references/integrations/aspnetcore/integration`, acesso 2026-11-19 — primária
- NuGet — páginas de `Scalar.AspNetCore` e da versão 2.16.16 (data de publicação), acesso 2026-11-19 — primária

**Confiança:** alta para registro, rota e exigência do scheme no documento; média para o nome do método de persistência na 2.16.16.

### 4. (R4) ProblemDetails: por endpoint ou por transformer global?

**Resposta:** As duas formas são suportadas no .NET 10. `ProducesProblem`/`ProducesValidationProblem` funcionam por endpoint **e por route group**, declarando status e content-types de erro. Existe também o caminho global: um operation transformer com `context.GetOrCreateSchemaAsync(typeof(ProblemDetails), ...)` (recurso do .NET 10) adiciona ao documento o schema `Error` e a resposta `4XX` com `application/problem+json` em todas as operações.

**Recomendação de uso:** global para 401/403 (a autenticação é uniforme), e `ProducesProblem` nos endpoints cujos códigos específicos fazem parte do contrato (400 de `Idempotency-Key` ausente, 404, 409, 422). Evita repetir a mesma resposta de auth em toda rota e mantém precisão onde o contrato nomeia o código.

**Fontes:**
- Microsoft Learn — *Customize OpenAPI documents* e *Include metadata* (aspnetcore-10.0), acesso 2026-11-19 — primária
- Context7 `/dotnet/aspnetcore.docs` — `customize-openapi.md` (exemplo do transformer global com `GetOrCreateSchemaAsync`), acesso 2026-11-19 — primária

**Confiança:** alta.

**Nota de contexto (R4 e além):** no .NET 10 o documento gerado é **OpenAPI 3.1** por padrão (`OpenApiVersion` permite fixar 3.0); os tipos de `Microsoft.OpenApi` no .NET 10 vêm do namespace `Microsoft.OpenApi` (no 9 era `Microsoft.OpenApi.Models`) — o código do transformer deve seguir a forma do 10.

---

## Recomendação

1. **O desenho segue** com document transformer (scheme) + operation transformer (requisito lido de `IAuthorizeData`, política = escopo; `AllowAnonymous` pulado), registrados em `AddOpenApi`, e `MapOpenApi()` no caminho padrão `/openapi/v1.json`.
2. **A decisão que a ADR-0013 precisa fechar:** para os escopos serem **formais** no documento, o scheme deve ser `OAuth2` (client credentials) ou `OpenIdConnect`; se a opção for `Http`/Bearer, os escopos ficam como descrição, não como requisito. Contexto para a decisão: o token local é emitido por `/dev/token` (não é um token endpoint OAuth2), e o alvo é Cognito (OIDC) — o `tokenUrl`/`openIdConnectUrl` do documento é configuração, e o teste de integração valida o que ele declara, não a disponibilidade do IdP.
3. **XML comments:** ligar é só garantir `GenerateDocumentationFile` (já ligado em `src/`); sem `AddOpenApi` com nome não-literal. O teste confirma se os handlers privados recebem descrição; se não, degrada para documento sem prosa, como a descoberta aceitou.
4. **Scalar:** `MapScalarApiReference` só em Development, com `AddDocument("v1")`; sem pré-preencher token (é demo, mas o aviso de segurança é real — quem quiser cola o token na própria UI).
5. **ProblemDetails:** transformer global para 401/403 + `ProducesProblem` onde o código é contrato.

## Questões residuais

- Se `OAuth2` for escolhido, decidir os `flows` (client credentials, token URL por ambiente) — a ADR-0013 é o lugar, e o teste fixa o que está declarado.
- Confirmar na prática (teste) se o comentário XML de um handler `private static` chega à operação; sem confirmação, o documento cumpre o esquema e perde a prosa.
