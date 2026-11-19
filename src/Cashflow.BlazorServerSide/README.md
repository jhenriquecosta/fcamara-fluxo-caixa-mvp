# Cashflow.BlazorServerSide

> O host das telas: login por cookie, perfis e a emissão do token de acesso no servidor.

## Por que existe

O navegador não pode guardar o token da API: um XSS o levaria junto. O host resolve sendo o único que o conhece — a sessão do navegador é um cookie `HttpOnly`, e cada chamada à API sai do servidor com um JWT de 5 minutos e só os escopos do perfil (o padrão BFF, ADR-0010).

## Finalidade

- **Login e logout**: usuários configurados, senha em hash PBKDF2, antiforgery no formulário, cookie `HttpOnly`/`SameSite=Lax` com 8 h deslizantes.
- **Perfis**: `operador` lança e estorna; `consulta` só lê. A tela esconde e bloqueia por policy; quem garante é a API, por escopo.
- **Emissão do token**: `AccessTokenIssuer` assina o JWT do perfil, e o `TokenManager` o mantém no circuito — nunca no HTML.
- **Painel de infraestrutura**: `/infraestrutura`, ligado só por `Infraestrutura:Enabled` (ADR-0011).

## Estilo arquitetural e padrões

- **BFF (Backend for Frontend)**: o host é cliente da API e esconde o token do navegador.
- **Composition Root do frontend**: registra os serviços do RCL e o emissor do token; as telas vêm do `Cashflow.Blazor.Modules`.
- **Sem trabalho de fundo**: publicar evento e consumir é do `BackgroundServer`.

## Dependências

Referencia `Cashflow.Blazor.Modules` e `Cashflow.Infrastructure`. Não referencia módulos de domínio — fala com a API por HTTP, com um endereço por serviço.

## Decisões

- [ADR-0010](../../docs/ultron/agents/adr/0010-frontend-blazor-bff.md): o token nunca chega ao navegador.
