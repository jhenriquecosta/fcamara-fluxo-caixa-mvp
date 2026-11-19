# PRD — Experiência de demonstração (task 0011)

| Campo | Conteúdo |
|---|---|
| **Documento** | PRD-0004 · Product Requirements Document |
| **Projeto** | fcamara-fluxo-caixa-mvp |
| **Autor** | ULTRON · skill ultron-prd — Operador: J Henrique Costa |
| **Data** | 2026-11-19 |
| **Status** | accepted |
| **Rastreabilidade** | Task 0011 · issue #27 · descoberta.md · Research #0003 · ADR-0011 · PRD-0003 |

> **Uma linha:** um comando sobe, testa e demonstra a solução inteira, e duas interfaces (web e terminal) mostram não só o negócio funcionando, mas a infraestrutura (mensageria, métricas e logs) que o sustenta.

## 1. Requisitos funcionais

| # | Requisito |
|---|---|
| RF1 | `./start.sh` (padrão `up`): confere pré-requisitos (docker, compose, dotnet, portas) → roda a suíte (`--sem-testes` pula) → sobe a stack → espera a saúde de cada serviço → aplica o ELK → imprime o painel com URLs, usuários de demo e próximos comandos |
| RF2 | `./start.sh test`: roda a suíte e mostra o resumo por projeto |
| RF3 | `./start.sh demo`: roteiro narrado contra a stack — crédito e débito → evento no outbox → consolidado projetado via Kafka → estorno → reenvio idempotente devolve o mesmo lançamento → métricas e logs do que acabou de acontecer |
| RF4 | `./start.sh terminal`: abre a UI de terminal. `status`, `logs [serviço]` e `down` completam o ciclo |
| RF5 | UI de terminal: menu com Saúde, Lançar, Lançamentos do dia (com estorno), Consolidado, Métricas, Logs, Demo e Sair. Perfil escolhido no início (operador/consulta) |
| RF6 | Front, página **Infraestrutura** (`/infraestrutura`): saúde por serviço, sinais do Prometheus, últimos logs filtráveis por serviço e atalhos para Grafana, Kibana e Prometheus. Atualiza a cada 10 s |
| RF7 | Todo bloco de infraestrutura degrada sozinho: serviço fora aparece "fora", sem quebrar o resto |

## 2. Não funcionais

- **Um cliente, duas UIs:** a lógica de consulta vive em `Cashflow.Blazor.Modules/Infraestrutura`, e o terminal não reimplementa nada.
- **Segurança:** o painel só liga com `Infraestrutura:Enabled`. O terminal só consegue token em Development (`/dev/token`). Os logs exibidos já são pseudonimizados na origem (`logging.md`).
- **Convenção 0009:** XML doc em todo membro público de `src/`. Testes nomeados pela regra, com `[Trait("Criterio", "ACn")]`.

## 3. Critérios de aceitação

1. `./start.sh` sobe a stack do zero, roda a suíte e termina com o painel de URLs e a saúde de cada serviço
2. `./start.sh demo` executa o fluxo ponta a ponta narrando cada passo
3. O painel Infraestrutura do front mostra saúde, métricas e logs reais da stack
4. A UI de terminal faz lançar, estornar, consultar consolidado, ver métricas e logs
5. Um serviço fora do ar aparece como fora, sem derrubar o painel nem o terminal
6. Suíte verde
