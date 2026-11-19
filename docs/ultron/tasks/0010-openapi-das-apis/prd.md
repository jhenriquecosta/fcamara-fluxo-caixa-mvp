# PRD — Superfície OpenAPI das APIs (task 0010)

| Campo | Conteúdo |
|---|---|
| **Documento** | PRD-0005 · Product Requirements Document |
| **Projeto** | fcamara-fluxo-caixa-mvp |
| **Autor** | ULTRON · skill ultron-prd — Operador: J Henrique Costa |
| **Data** | 2026-11-19 |
| **Status** | accepted |
| **Rastreabilidade** | Task 0010 · issue #26 · descoberta.md · Research #0004 · ADR-0013 · PRD-0003 |

> **Uma linha:** as duas APIs publicam o contrato OpenAPI gerado pelo pacote nativo, sempre disponível e anônimo, com esquema OAuth2 e os escopos por rota, `Idempotency-Key` nos POSTs e `ProblemDetails` nas respostas de erro — e uma UI Scalar só em Development.

## O problema

Quem consome as APIs não tem contrato: para integrar, é preciso ler o código ou experimentar as rotas. A referência do ADR-0009 expõe Swagger via Swashbuckle, e este projeto diverge dela usando o gerador nativo do .NET 10 sem que a divergência esteja registrada. A autorização é por escopo em claim, mas nada disso aparece em lugar nenhum publicável.

## A solução

O `Cashflow.WebAPI` gera o documento com o `Microsoft.AspNetCore.OpenApi` e o publica em `/openapi/v1.json`, sem autenticação para lê-lo. O documento declara o esquema OAuth2 (client credentials) com os três escopos, exige por operação o escopo da política de autorização correspondente, documenta o `Idempotency-Key` nos dois POSTs e as respostas de erro em `ProblemDetails`. Uma UI Scalar, só em Development, renderiza o documento para exercitar as rotas. A decisão está na ADR-0013, fundamentada no Research #0004.

## Histórias de usuário

1. Como integrador, quero ler `/openapi/v1.json` na API de Lançamentos, para integrar sem ler o código.
2. Como integrador, quero ler o mesmo documento na API de Consolidado, para integrar a leitura do saldo.
3. Como integrador, quero que cada documento liste só as rotas do módulo daquele deploy, para não chamar uma rota que aquele processo não serve.
4. Como integrador, quero ver o esquema de segurança com os escopos declarados, para saber quais credenciais pedir.
5. Como integrador, quero que cada operação diga o escopo que exige, para pedir só o necessário.
6. Como integrador, quero que a operação sem autenticação apareça sem requisito de segurança, para não mandar token à toa.
7. Como integrador, quero o `Idempotency-Key` declarado nos POSTs, para saber que a chave é obrigatória antes de chamar.
8. Como integrador, quero as respostas de erro em `application/problem+json`, para tratar o erro com um formato só.
9. Como integrador, quero 401 e 403 documentados, para tratar sessão e permissão sem adivinhar.
10. Como operador, quero a UI Scalar em Development, para exercitar as rotas sem montar `curl` à mão.
11. Como arquiteto, quero a divergência do Swashbuckle da referência registrada, para que a próxima pessoa não a "corrija" de volta sem conhecer a razão.
12. Como mantenedor, quero as descrições vindas dos comentários XML que o código já exige, para não manter duas fontes da mesma prosa.
13. Como mantenedor, quero um teste que reprove quando o documento sair do contrato, para que a documentação não apodreça em silêncio.
14. Como responsável pela entrega, quero o documento anônimo, para que a banca o leia sem credencial.

## Decisões de implementação

- **Geração nativa.** `AddOpenApi`/`MapOpenApi` no host da API; documento padrão em `/openapi/v1.json`, OpenAPI 3.1. Sem Swashbuckle. (ADR-0013)
- **Onde o registro vive.** Uma extensão no projeto compartilhado de infraestrutura web (o mesmo binário serve os dois deploys), chamada pelo host da API; a UI é mapeada condicionalmente a Development.
- **Esquema OAuth2 client credentials** com os três escopos (`lancamentos.write`, `lancamentos.read`, `consolidado.read`) e `tokenUrl` configurável (`OpenApi:TokenUrl`); no local, aponta para o `/dev/token` como aproximação de desenvolvimento.
- **Requisito por operação** por um operation transformer que lê `IAuthorizeData` da metadata do endpoint: a política é o escopo; `AllowAnonymous` não recebe requisito. O scheme é registrado por um document transformer.
- **`Idempotency-Key`** declarado por metadata nos dois POSTs (registrar e estornar), com o 400 de chave ausente; o header é lido de `request.Headers`, não é parâmetro de assinatura.
- **`ProblemDetails`** por um transformer global para 401/403 e por `ProducesProblem` onde o código é contrato (400, 404, 409, 422).
- **Descrições dos XML docs** pela integração automática do pacote (o `GenerateDocumentationFile` já está ligado em `src/`); sem `.WithSummary(...)` manual. Residual conhecido: a associação para handlers `private static` não está documentada — se não associar, o documento cumpre o esquema e perde a prosa (D5 da descoberta).
- **UI Scalar 2.16.16**, `MapScalarApiReference` com `AddDocument("v1")`, só em Development; sem pré-preencher token.
- **Um documento por deploy** é consequência do `Modules:<X>:Enabled` — o processo só mapeia as rotas do módulo ligado.
- Pacotes novos: `Microsoft.AspNetCore.OpenApi` 10.0.x e `Scalar.AspNetCore` 2.16.16 (sem dependências transitivas).

## Decisões de teste

- **Bom teste aqui** lê o JSON do documento pelo cliente HTTP do host, depois de subir o WebAPI com o módulo escolhido — afirma o comportamento observável (rotas, esquema, escopos, header, respostas), nunca os transformers por dentro. Refatorar o transformer não deve quebrar o teste; mudar o contrato, sim.
- **Seam único:** o `WebApiFactory` que os testes de integração já usam (`SoLancamentos`/`SoConsolido`), com ambiente `Testing`; a UI é conferida com ambiente `Development` pelo mesmo factory.
- **Módulos testados:** o host com cada módulo ligado — o documento de Lançamentos e o de Consolidado, cada um no seu teste.
- **Prior art:** os testes de integração existentes que sobem o host pelo `WebApiFactory` e afirmam respostas HTTP.

## Avaliação de Dependências (SCA)

Sem lockfile no repositório — seção não aplicável. Os dois pacotes que a feature acrescenta são o `Microsoft.AspNetCore.OpenApi` 10.0.x e o `Scalar.AspNetCore` 2.16.16 (a 2.16.16 não tem dependências transitivas).

## Fora de escopo

- Trocar a autenticação ou o emissor de token (o Cognito entra no alvo, ADR-0010/ADR-0013).
- Gerar clientes/SDK a partir do documento.
- Publicar o documento em portal, registry ou geração em build-time.
- Versionamento da API (`v2`) e múltiplos documentos por deploy.
- UI interativa fora de Development.

## Critérios de aceitação

- [AC1] `/openapi/v1.json` responde 200 no deploy de Lançamentos e lista só as rotas de Lançamentos
- [AC2] `/openapi/v1.json` responde 200 no deploy de Consolidado e lista só as rotas de Consolidado
- [AC3] O documento declara o esquema OAuth2 client credentials com os três escopos, e cada operação protegida exige o escopo da sua política; operação `AllowAnonymous` sai sem requisito
- [AC4] Os dois POSTs declaram `Idempotency-Key`, com o 400 de chave ausente; as respostas de erro usam `application/problem+json` (401 e 403 no mínimo)
- [AC5] `/scalar` responde em Development e não é mapeado fora de Development
- [AC6] Suíte verde
