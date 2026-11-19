# ADR-0010 — Frontend Blazor Server como BFF, com o host fazendo o papel de IdP local

**Status:** accepted
**Data:** 2026-11-19
**Decisores:** J Henrique Costa (Operador) · ULTRON
**Versão do documento:** 1.0
**Relacionados:** ADR-0001 (dois serviços), ADR-0004 (idempotência e estorno), ADR-0005 (projeção), ADR-0009 (monólito modular) · Research #0002 · PRD-0003

## Contexto

A entrega ganhou um frontend (task 0008). A API autentica por JWT assinado com chave simétrica (`Auth:Issuer`, `Auth:Audience`, `Auth:SigningKey`) e autoriza por escopo (`lancamentos.write`, `consolidado.read`). O único emissor de token hoje é o `/dev/token`, que só existe em `Development` e entrega qualquer escopo a qualquer um.

A arquitetura-alvo prevê pessoas autenticando por **authorization code + PKCE** num IdP gerenciado. Esse IdP não existe no MVP local. A referência (Practical.CleanArchitecture) resolve com IdentityServer + OIDC no `ClassifiedAds.BlazorServerSide`.

## Decisão

1. **Dois projetos, no molde da referência.** `Cashflow.Blazor.Modules` (RCL: `Core/Services/HttpService`, `ITokenManager`, uma pasta por módulo com `Models`/`Services`/`Pages`) e `Cashflow.BlazorServerSide` (host: `Program`, `ConfigurationOptions`, login/logout, `TokenManager`). O RCL só conhece HTTP: não referencia módulos, domínio, aplicação nem infraestrutura.
2. **Blazor Server é o BFF.** O navegador recebe só o cookie de sessão (`HttpOnly`). O access token nasce e morre no servidor, dentro do circuito.
3. **O host é o IdP local.** O login valida usuário e senha contra `Auth:Users` (hash PBKDF2), grava no cookie as claims `sub`, `name`, `comercianteId` e `perfil`, e o `TokenManager` emite, por chamada, um JWT de **5 minutos** com o issuer, a audience e a chave que a API já valida, contendo só os escopos do perfil.
4. **Dois perfis**: `operador` (lança, estorna, consulta) e `consulta` (lê). A UI esconde e bloqueia por policy; quem garante é a API, por escopo.
5. **Escopo novo `lancamentos.read`** para o `GET /api/v1/lancamentos`. Ler com `lancamentos.write` daria ao perfil consulta o poder de escrever ou, ao contrário, o deixaria sem lista.

## Alternativas rejeitadas

| Alternativa | Por que não |
|---|---|
| Keycloak no compose + OIDC code/PKCE | Fiel ao alvo, mas custa ~700 MB de RAM num compose que já roda Elasticsearch e Kibana. Também exigiria a API em modo duplo (JWKS para o front, chave simétrica para os testes) e a conciliação do `issuer` entre a rede do compose e o navegador. O custo de risco na véspera da entrega supera o ganho de demonstração |
| Password grant (ROPC) na API | Removido no OAuth 2.1, e a API passaria a ver senha de usuário |
| Token no navegador (WASM ou `ProtectedLocalStorage`, como a referência) | Expõe o token a XSS sem benefício: o Blazor Server já executa no servidor |
| Front chamando `/dev/token` | Endpoint de desenvolvimento, sem credencial, com todos os escopos |

## Consequências

- **Positivas:** login e autorização funcionam ponta a ponta localmente, sem infraestrutura nova. O token tem vida curta e escopo mínimo. A divisão de papéis (UI conveniência, API barreira) é testável.
- **Negativas:** o host do front e a API compartilham a chave de assinatura simétrica. Quem tem o host consegue emitir token. É aceitável num MVP local, e **inaceitável em produção**: lá a chave não existe no front.
- **Caminho de saída para o alvo (Cognito/OIDC):** trocar, no host, o login por formulário por `AddOpenIdConnect` (code + PKCE, `SaveTokens`), e o `TokenManager` passa a devolver o access token da sessão em vez de emiti-lo, como o `TokenManager` da referência. Na API, `CashflowAuth` troca a chave simétrica por `Authority`/JWKS. As páginas e os serviços do `Cashflow.Blazor.Modules` não mudam, porque dependem só de `ITokenManager`.
