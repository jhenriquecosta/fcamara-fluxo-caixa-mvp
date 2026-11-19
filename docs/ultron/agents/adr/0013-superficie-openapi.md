# ADR-0013 — Superfície OpenAPI nativa, documento público e escopos formais

**Status:** accepted
**Data:** 2026-11-19
**Decisores:** J Henrique Costa (Operador) · ULTRON
**Relacionados:** ADR-0009 (monólito modular e referência) · ADR-0010 (frontend BFF e `/dev/token`) · Research #0004 (`docs/ultron/tasks/0010-openapi-das-apis/research.md`)

## Contexto

Nenhuma API publica o contrato. A referência do ADR-0009 (ClassifiedAds.WebAPI) expõe Swagger via Swashbuckle, e este projeto já diverge dela sem registrar a divergência: a superfície é gerada pelo `Microsoft.AspNetCore.OpenApi` nativo do .NET 10. A autorização é por escopo em claim — `lancamentos.write`, `lancamentos.read` e `consolidado.read` —, e as políticas de autorização **são** os escopos (`Scopes.*`). O token local é emitido pelo `/dev/token` (ADR-0010), que não é um endpoint OAuth2; o alvo é o Cognito.

O Research #0004 estabeleceu dois fatos que decidem o desenho: o esquema de segurança é registrado por um *document transformer* e o requisito por operação por um *operation transformer*, que lê `IAuthorizeData` da metadata do endpoint; e a especificação OpenAPI só carrega escopos formais em esquema `OAuth2` ou `OpenIdConnect` — num esquema HTTP Bearer, o array de escopos do requisito é obrigatoriamente vazio.

## Decisão

1. **Geração nativa.** O documento OpenAPI é gerado pelo `Microsoft.AspNetCore.OpenApi` e mapeado em `/openapi/v1.json`, **sempre disponível e anônimo** — é o contrato que o consumidor lê, e não carrega segredo. A UI interativa é o **Scalar** (`Scalar.AspNetCore` 2.16.16), mapeada **só em Development**.
2. **Escopos formais.** O esquema de segurança é `OAuth2` com o fluxo **client credentials** e os três escopos declarados; o `tokenUrl` é configuração (`OpenApi:TokenUrl`) e, no ambiente local, aponta para o `/dev/token` como aproximação de desenvolvimento. Cada operação recebe o requisito com o escopo lido da política de autorização (`IAuthorizeData`) por um operation transformer; endpoints `AllowAnonymous` são pulados.
3. **Contrato do corpo e do erro.** O header `Idempotency-Key` é declarado por metadata nos dois POSTs, com o 400 de chave ausente; `ProblemDetails` entra no documento por um transformer global para 401/403 e por `ProducesProblem` onde o código de erro é parte do contrato (400, 404, 409, 422).
4. **Descrição sem duplicação.** As descrições vêm dos comentários XML que a convenção 0009 já exige, pela integração automática do pacote (`GenerateDocumentationFile`); não há `.WithSummary(...)` escrito à mão.
5. **Um documento por deploy é consequência.** Cada processo só mapeia as rotas do módulo ligado (`Modules:<X>:Enabled`), então o documento expõe exatamente o que aquele deploy serve.

## Alternativas rejeitadas

| Alternativa | Por que não |
|---|---|
| Swashbuckle + Swagger UI | É o ecossistema da referência, do qual o projeto diverge; o pacote nativo gera o mesmo documento e o Scalar o renderiza sem trazer o Swashbuckle de volta |
| NSwag | Geração própria, redundante ao `AddOpenApi`/`MapOpenApi` do .NET 10 |
| Esquema HTTP Bearer | Fiel ao formato do header, mas a especificação obriga escopos vazios fora de OAuth2/OIDC — o documento deixaria de declarar os escopos por rota, que é o critério |
| Esquema OpenIdConnect | Os escopos vêm da descoberta do IdP, não do documento, e não há IdP no ambiente local; o documento deixaria de se bastar |
| Documento só em Development | Esconde o contrato de quem deveria consumi-lo; o objetivo é expor |
| Summaries manuais por endpoint | Duas fontes da mesma prosa; os comentários XML já são a fonte verdadeira |

## Consequências

**Positivas**

- O contrato fica publicado nas duas APIs, sempre, sem autenticação para lê-lo.
- Os escopos por rota são derivados das próprias políticas — uma rota nova herda a documentação da autorização que já declara.
- As descrições acompanham o código sem manutenção paralela.
- A UI é conveniência de desenvolvimento, sem acoplar o servidor a ela.

**Negativas / trade-offs aceitos**

- O `tokenUrl` do documento é uma afirmação sobre um IdP: no local aponta para o `/dev/token`, que não cumpre o protocolo OAuth2 — o fluxo "authorize" da UI não completa contra ele, e o token local continua sendo obtido como o ADR-0010 descreve.
- A lista de escopos do esquema é declarada uma vez no fluxo, separada das constantes de `Scopes` usadas pelos requisitos; mudança de escopo exige tocar os dois lados, e o teste de integração é o guarda.
- O documento passa a expor o inventário de rotas de cada deploy a quem o alcançar.

**Follow-ups**

- Teste de integração do documento: rotas do módulo ligado, esquema com escopos, `Idempotency-Key` nos POSTs e `ProblemDetails` nas respostas de erro.
- Atualizar o README com o caminho do documento e da UI (Development).
- Quando o Cognito entrar, o `tokenUrl` passa a ser configuração do ambiente, sem mudança de código.
