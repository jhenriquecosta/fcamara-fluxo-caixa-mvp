# Research #0002 — Blazor Server como frontend do fluxo de caixa

**Data:** 2026-11-19 · **Task:** 0008 · **Questões:** as abertas pela descoberta (G1, G3, G6)
**Context7/Tavily:** não usados. As duas fontes primárias são o próprio template do SDK instalado e o código da referência, mais confiáveis que um resumo da web para estas perguntas.

## 1. Como a referência estrutura o frontend Blazor?

**Resposta:** em dois projetos.
- `ClassifiedAds.Blazor.Modules` (Sdk.Razor): `Core/Services/HttpService` (base que injeta o bearer via `ITokenManager`), `Core/Services/TokenManager.cs` (a interface `ITokenManager` + `TokenModel`), `Core/Components/ConfirmDialog`, e uma pasta por módulo com `Models/`, `Services/<X>Service : HttpService`, `Pages/` (`.razor` + code-behind `.cs`) e `Components/`.
- `ClassifiedAds.BlazorServerSide` (Sdk.Web): `Program.cs`, `ConfigurationOptions/AppSettings` (`OpenIdConnect`, `ResourceServer`), `Services/TokenManager` (implementação no host), `Pages/Login`/`Logout`, `Shared/MainLayout`/`NavMenu`. O host autentica por cookie + OpenID Connect (code flow) contra o IdentityServer.

**Fonte:** `src/Monolith/ClassifiedAds.Blazor.Modules` e `src/Monolith/ClassifiedAds.BlazorServerSide` de jhenriquecosta/Practical.CleanArchitecture (sparse-checkout em 2026-11-19, lidos arquivo a arquivo).

**Divergências que adotaremos:**
- a referência está em net8 com `_Host.cshtml` (modelo antigo). Usaremos o modelo **Blazor Web App** do .NET 10 (`App.razor` + `Routes`, `AddInteractiveServerComponents`), porque o .NET 8 saiu de suporte em 10/11/2026 (Research #0001);
- o `TokenManager` da referência guarda tokens em `ProtectedLocalStorage`. O nosso não guarda nada no navegador (G6).

## 2. Como fazer login com cookie num Blazor Web App .NET 10 com interatividade Server?

**Resposta:** a `SignInAsync` exige `HttpContext` e, portanto, uma requisição HTTP real, não o circuito SignalR. A página de login é **SSR estática**: `[ExcludeFromInteractiveRouting]` + `EditForm` com `FormName` + `[SupplyParameterFromForm]` + `[CascadingParameter] HttpContext`. As demais páginas usam `InteractiveServer` por meio de `<Routes @rendermode="PageRenderMode" />`, com `HttpContext.AcceptsInteractiveRouting()`. É o mesmo padrão do template `dotnet new blazor --auth Individual`. O `[Authorize]` nas páginas vira metadado de endpoint, e o cookie auth redireciona para `LoginPath` antes da renderização.

**Fonte:** template `blazor` do SDK 10.0.115 instalado nesta máquina (`dotnet new blazor --interactivity Server`), gerado e lido em 2026-11-19, e o padrão de contas do mesmo template.

## 3. Quem emite o access token que a API aceita, sem IdP?

**Alternativas pesadas:**
- **Keycloak no compose (OIDC code + PKCE).** Fiel ao alvo, mas soma ~700 MB de RAM a um compose que já tem Elasticsearch e Kibana. Também exigiria a API em modo duplo (JWKS × chave simétrica dos testes) e esbarra no problema clássico de `issuer` diferente entre a rede interna e o navegador. Custo alto na véspera da entrega (20/11).
- **ROPC (password grant) na API.** Descartado: o OAuth 2.1 removeu o fluxo, e ele ensinaria à banca o padrão errado.
- **O host Blazor como IdP local, com a mesma chave/issuer que a API já valida.** O token nasce no servidor, vale 5 minutos e carrega os escopos do perfil. É o que o `/dev/token` já faz, só que atrás de credencial e com escopo mínimo.

**Escolha:** a terceira, registrada no ADR-0010 com o caminho de saída para OIDC.

## 4. Como guardar a senha?

`Microsoft.AspNetCore.Identity.PasswordHasher<T>` (PBKDF2, HMAC-SHA512, 100 mil iterações no formato V3). Está no shared framework `Microsoft.AspNetCore.App`, sem pacote extra.
