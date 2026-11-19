# Descoberta — task 0010-openapi-das-apis (issue #26)

**Data:** 2026-11-19 · **Tipo:** feature · **Referências:** ADR-0009 (monólito modular), ADR-0010 (frontend como cliente) · PRD-0003/0004

## O problema

Nenhuma API publica o contrato. A referência (ClassifiedAds.WebAPI do ModularMonolith) expõe Swagger com esquema Bearer, e a divergência que este projeto já pratica — `Microsoft.AspNetCore.OpenApi` nativo, sem Swashbuckle — não está declarada em lugar nenhum.

## Decisões da descoberta

| # | Decisão | Por quê |
|---|---|---|
| D1 | **UI: Scalar** (`Scalar.AspNetCore`), nunca Swagger UI | O documento é gerado pelo pacote nativo; o Scalar só o renderiza. O Swagger UI puxaria o Swashbuckle de volta — a peça da referência que este projeto deixa — e uma dependência só já está no cache |
| D2 | **Documento `/openapi/v1.json` sempre disponível e anônimo; UI só em Development** | O documento é o contrato, e é o que um consumidor lê; não carrega segredo (o Bearer aparece como esquema). A UI é conveniência de desenvolvimento |
| D3 | **Escopos documentados automaticamente** | As políticas de autorização **são** os escopos (`Scopes.*`), e o endpoint carrega `IAuthorizeData`; um operation transformer traduz a política em requisito de segurança com o escopo, e documenta 401/403 — sem anotação manual que envelhece |
| D4 | **`Idempotency-Key` documentado nos dois POSTs por metadata** | A chave é lida de `request.Headers` no endpoint, não é parâmetro; um marcador de metadata no mapeamento da rota permite ao transformer declarar o header e o 400 de chave ausente, sem inventar parâmetro que a assinatura não tem |
| D5 | **XML docs ligados como descrição do documento; sem summary manual** | A convenção 0009 já obriga XML doc em todo membro público; duplicar a prosa em `.WithSummary(...)` criaria duas fontes. Se a integração não alcançar os handlers, o documento sai com esquema e sem descrição — ainda cumpre "só expor" |
| D6 | **ADR-0013 própria declara a divergência** | O ADR-0009 decide a estrutura do monólito; a superfície OpenAPI é decisão própria, com alternativas próprias (Swashbuckle, NSwag, nativo) e consequências próprias |
| D7 | **Um documento por deploy é consequência, não configuração** | Cada deploy só mapeia as rotas do módulo ligado (`Modules:<X>:Enabled`); o documento gerado reflete exatamente o que o processo expõe |

## Desenho de encaixe (a confirmar no research/PRD)

- O registro vive em `Cashflow.Infrastructure/Web` (`AddCashflowOpenApi`/`UseCashflowOpenApi`), chamado pelo `Cashflow.WebAPI/Program.cs` — o mesmo binário serve os dois deploys.
- Pacotes: `Microsoft.AspNetCore.OpenApi` 10.0.x (10.0.9 no cache) e `Scalar.AspNetCore` 2.16.16 (no cache).
- Teste de integração sobe o host e confere o documento (rotas do módulo ligado, Bearer com escopo, `Idempotency-Key`, `ProblemDetails`).

## Questões abertas — para o `/ultron-research`

| # | Questão |
|---|---|
| R1 | No `Microsoft.AspNetCore.OpenApi` 10.0.x, qual a forma canônica de declarar o security scheme e os requisitos por operação (document transformer × operation transformer), e como um operation transformer lê `IAuthorizeData` |
| R2 | No .NET 10, como os comentários XML chegam a endpoints de minimal API declarados como métodos privados (e o que liga isso: propriedade do csproj, source generator, opção) |
| R3 | `Scalar.AspNetCore` 2.16.16 com .NET 10: `MapScalarApiReference` e como a UI apresenta o Bearer/autorização |
| R4 | Declarar os `ProblemDetails` por endpoint (`.ProducesProblem`) × um transformer global que adiciona as respostas de erro comuns |

## Alternativas rejeitadas

| Alternativa | Por que não |
|---|---|
| Swagger UI + Swashbuckle | É a peça da referência que o projeto deixa; mais um ecossistema para o mesmo documento |
| `NSwag` | Geração própria, fora do pacote nativo; o .NET 10 já traz `AddOpenApi`/`MapOpenApi` |
| Summaries manuais por endpoint | Duas fontes da mesma prosa; a convenção 0009 já tem a fonte verdadeira |
| Documento só em Development | Esconde o contrato de quem deveria consumi-lo; o "objetivo é só expor" aponta ao contrário |
