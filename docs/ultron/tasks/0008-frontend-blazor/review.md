# Revisão de conformidade — task 0008 (frontend Blazor Server)

**Data:** 2026-11-19 · **Skill:** ultron-review · **Base:** `origin/develop` · **Commits:** `8542345`..HEAD
**Referências:** PRD-0003, ADR-0010, ADR-0009 (mecanismo da referência), ADR-0004 (idempotência), `logging.md`

## Veredito

**Sem achado crítico.** Pode seguir para o fechamento.

## Conformidade

| Referência | Conferido | Situação |
|---|---|---|
| PRD RF1 | cookie `HttpOnly`, `SameSite=Lax`, 8 h deslizantes; mensagem única para usuário inexistente e senha errada; `ReturnUrl` só local | ✓ (testes AC1, RF1) |
| PRD RF2/RF3 | `[Authorize]` em toda página do RCL; `/logout` só por POST com antiforgery | ✓ |
| PRD RF5–RF8 | lista do dia, novo (só operador), estorno com confirmação, consolidado por data com aviso de consistência eventual | ✓ |
| PRD RF6 / ADR-0004 | chave por intenção: reaproveitada em 503/409, trocada após recusa definitiva ou sucesso; no estorno, uma chave por lançamento até concluir | ✓ |
| PRD RF9 | ProblemDetails → mensagem; 401 → login; API fora → "serviço indisponível"; corpo inválido → 502 legível | ✓ |
| PRD RF10 | `GET /api/v1/lancamentos` como `IQuery` + handler, pelo `Dispatcher`, escopo `lancamentos.read` | ✓ (ArchTest `Modulos` cobre a regra) |
| ADR-0010 §1 | RCL sem módulo, domínio, aplicação nem infraestrutura | ✓ (ArchTest `Frontend`) |
| ADR-0010 §2–3 | token só no servidor, 5 min, escopos do perfil | ✓ (AC6, `TokenTests`) |
| ADR-0009 | estrutura e mecanismo da referência (`HttpService` + `ITokenManager` + serviço por módulo + páginas `.razor`/`.cs`) | ✓; as divergências (Blazor Web App .NET 10, token fora do `ProtectedLocalStorage`) estão declaradas no Research #0002 |
| `logging.md` | host com `UseCashflowLogging("web")`; login loga só perfil e recusa, nunca usuário ou senha | ✓ |

## Achados médios (registrados, não bloqueiam)

1. **Sem rate limit no `POST /login`.** Força bruta contra os usuários de demo é possível localmente. O PBKDF2 (100 mil iterações) encarece cada tentativa. No alvo, o login é do IdP (Cognito tem proteção própria) e o WAF da borda limita. Fica fora do MVP.
2. **O token em cache no circuito sobrevive ao logout em outra aba por até 5 minutos.** Esse é o limite de vida do token, e o circuito cai na próxima navegação com recarga. Aceito pelo ADR-0010 (vida curta como mitigação).
3. **Chave de assinatura compartilhada entre o front e a API.** Consequência negativa declarada no ADR-0010, com o caminho de saída para OIDC.

## Fora do que esta revisão afirma

O `docker compose up` completo não foi exercitado nesta sessão (ver `task.md`, Verificação manual). O Dockerfile foi validado por `docker build`.
