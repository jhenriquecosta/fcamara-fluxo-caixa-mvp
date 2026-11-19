# Descoberta — Frontend Blazor Server (task 0008)

**Data:** 2026-11-19 · **Modo:** autônomo (o Operador delegou cada gap à melhor abordagem) · **Issue:** #24

## O que muda na solução

O `solution.md` dizia *"não tem interface gráfica — a entrega é API e documentação"*. O Operador decidiu acrescentar um frontend como diferencial. O que não muda: a API continua sendo o produto, e o frontend é **mais um cliente** dela, com o mesmo contrato HTTP e os mesmos escopos que um ERP ou um script usariam.

## Domínios tocados

`dominios: lancamentos, consolidado` — **consumidor**, via HTTP. O frontend não referencia nenhum `Cashflow.Modules.*`. Ele também não referencia `Domain` nem `Application`: se compartilhasse tipos com eles, um refactor interno quebraria a UI. O repositório não tem `docs/ultron/agents/contracts/`, então o gate de contratos não se aplica. O contrato é o OpenAPI implícito dos endpoints `/api/v1/*`, e o PRD-0002 congela os campos usados.

## Gaps encontrados e decisão tomada

| # | Gap | Decisão | Por quê |
|---|---|---|---|
| G1 | A referência tem `ClassifiedAds.Blazor.Modules` (RCL) + `ClassifiedAds.BlazorServerSide` (host) | Espelhar: `Cashflow.Blazor.Modules` + `Cashflow.BlazorServerSide` | Memória do projeto: seguir a referência de verdade, com o mecanismo (`HttpService` + `ITokenManager` + serviço por módulo + páginas por módulo) |
| G2 | Não há endpoint para listar lançamentos, e o estorno precisa do id | `GET /api/v1/lancamentos?data=` como `IQuery` + handler no módulo Lançamentos, com o escopo novo `lancamentos.read` | Ler com escopo de escrita confundiria os papéis. O perfil *consulta* precisa listar sem poder lançar |
| G3 | Não há IdP. A API valida JWT HMAC (`Auth:*`), e o único emissor é o `/dev/token`, que só existe em Development | O host Blazor faz o papel de IdP local: login por formulário → cookie de sessão → access token curto emitido no servidor por chamada à API | ADR-0010. A troca para OIDC (Cognito no alvo AWS, IdentityServer na referência) fica restrita ao `TokenManager`, à configuração de autenticação do host e ao login: as páginas, os serviços e a API não mudam |
| G4 | Autorização | Dois perfis: **operador** (lançar, estornar, consultar) e **consulta** (só consultar). A UI esconde e bloqueia por policy; a API é quem garante, por escopo | A UI é conveniência, a API é a barreira. O critério de aceite testa as duas |
| G5 | Usuários | Lista configurada (`Auth:Users`), com senha em hash PBKDF2 (`PasswordHasher` do ASP.NET Core Identity) e `comercianteId` por usuário | O MVP não tem cadastro de pessoas, que fica fora de escopo. Hash, nunca texto puro, nem em dev |
| G6 | Onde vive o token | Só no servidor (circuito Blazor). O navegador tem apenas o cookie `HttpOnly` | Blazor Server já é um BFF; levar token ao browser abriria exfiltração por XSS sem ganho nenhum |
| G7 | Consolidado eventualmente consistente | A UI avisa que o saldo reflete em segundos e oferece *atualizar* | ADR-0005: o consolidado é derivado; a UI não pode prometer o que a arquitetura não garante |
| G8 | Idempotência | A UI gera a `Idempotency-Key` **por intenção de envio** (uma por formulário aberto ou por clique de estorno), não por tentativa | Duplo clique ou retry reenviam a mesma chave, e a API devolve o mesmo 201 (ADR-0004) |
| G9 | Compose | Serviço `web` (porta 8090), lendo `lancamentos-api` e `consolidado-api` por endpoints separados | Falha isolada: com o consolidado fora, a tela de lançamentos continua funcionando (ADR-0001) |
| G10 | Visual | Bootstrap do template .NET 10, sem CDN, sem biblioteca de componentes | "Limpo": zero dependência de terceiros no front, alinhado ao resto da stack |

## Fora de escopo

Cadastro de usuários e comerciantes · refresh token · i18n · relatórios de período · edição de lançamento (não existe por definição: estorno é um novo lançamento).
