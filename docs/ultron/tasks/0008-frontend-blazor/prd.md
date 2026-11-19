# PRD — Frontend Blazor Server (task 0008)

| Campo | Conteúdo |
|---|---|
| **Documento** | PRD-0003 · Product Requirements Document |
| **Projeto** | fcamara-fluxo-caixa-mvp |
| **Autor** | ULTRON · skill ultron-prd — Operador: J Henrique Costa |
| **Data** | 2026-11-19 |
| **Versão do documento** | 1.0 |
| **Status** | accepted |
| **Rastreabilidade** | Task 0008 · issue #24 · descoberta.md · Research #0002 · ADR-0010 |

> **Uma linha:** uma interface web em Blazor Server, cliente da API como qualquer outro, que deixa o comerciante entrar, lançar, estornar e ver o consolidado do dia, com a autorização garantida pela API e não pela tela.

## 1. Problema

A entrega era só API e documentação. Para a banca, ver o fluxo funcionando ponta a ponta numa tela (lançar, ver o evento virar consolidado, estornar e ver o saldo voltar) comunica a arquitetura mais rápido do que um `curl`. A tela não pode, porém, furar o desenho: ela não acessa banco, não referencia módulo e não carrega regra de negócio.

## 2. Usuários e perfis

| Perfil | Pode | Escopos no token |
|---|---|---|
| **operador** | entrar, lançar, estornar, listar, consultar consolidado | `lancamentos.write lancamentos.read consolidado.read` |
| **consulta** | entrar, listar, consultar consolidado | `lancamentos.read consolidado.read` |

Cada usuário pertence a **um** comerciante (`comercianteId` no token). Um comerciante não enxerga o dado de outro, porque a API filtra pelo token.

## 3. Requisitos funcionais

| # | Requisito |
|---|---|
| RF1 | `/login`: usuário e senha. Credencial inválida mostra erro genérico, sem dizer qual campo errou. Credencial válida cria a sessão por cookie `HttpOnly`, `SameSite=Lax`, com 8 h deslizantes, e volta para a `returnUrl` local |
| RF2 | Toda rota fora de `/login` exige sessão; sem ela, redireciona para `/login?ReturnUrl=…` |
| RF3 | `POST /logout` (antiforgery) encerra a sessão |
| RF4 | `/` (início): consolidado de hoje + atalhos permitidos ao perfil |
| RF5 | `/lancamentos`: lançamentos de um dia (padrão: hoje), com tipo, valor, descrição, se é estorno e se já foi estornado |
| RF6 | `/lancamentos/novo` (só operador): valor > 0, tipo crédito/débito, descrição opcional (≤ 200). A `Idempotency-Key` nasce com o formulário, e o reenvio do mesmo formulário reaproveita a chave |
| RF7 | Estornar (só operador, só lançamento não estornado e que não é estorno): confirmação → `POST /estornos` com chave própria → a lista atualiza |
| RF8 | `/consolidado`: escolhe a data e mostra saldo do dia, saldo anterior e acumulado, com o aviso de consistência eventual |
| RF9 | Erros da API (`ProblemDetails`) aparecem como mensagem legível; 401 da API leva ao login |
| RF10 | API: `GET /api/v1/lancamentos?data=yyyy-MM-dd` (escopo `lancamentos.read`). Sem `data`, usa o dia de negócio (America/Sao_Paulo). Lista só o comerciante do token |

## 4. Requisitos não funcionais

- **Segurança:** token só no servidor, emitido por chamada com validade de 5 min e escopo mínimo do perfil. Senha em hash PBKDF2 (`PasswordHasher`). O antiforgery do framework cobre os formulários SSR. Chave de assinatura só por configuração ou variável de ambiente, nunca no código.
- **Isolamento:** endpoints separados para Lançamentos e Consolidado. Com o consolidado fora, `/lancamentos` continua funcionando, e `/consolidado` mostra o erro sem derrubar o circuito.
- **Arquitetura:** `Cashflow.Blazor.Modules` não referencia `Cashflow.Modules.*`, `Domain`, `Application` nem `Infrastructure`. A regra é congelada em ArchTest.
- **Observabilidade:** log Serilog do host (mesma convenção dos demais) e `/health`.

## 5. Critérios de aceitação

1. Rota protegida sem sessão redireciona para /login; credencial inválida não autentica
2. Perfil consulta não vê nem acessa lançar/estornar; a API recusa (403) mesmo se chamada
3. Lançamento registrado pela UI aparece na lista do dia
4. Estorno pela UI cria o lançamento inverso e marca o original como estornado
5. Consolidado do dia exibido com saldo do dia, anterior e acumulado
6. O token de acesso nunca chega ao navegador (Blazor Server como BFF)
7. Suíte verde (unitários + integração)

## 6. Fora de escopo

Cadastro de usuários · refresh token · OIDC real (caminho no ADR-0010) · relatórios de período · edição de lançamento.
