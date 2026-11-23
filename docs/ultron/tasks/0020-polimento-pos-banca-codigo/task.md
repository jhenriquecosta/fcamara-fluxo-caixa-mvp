# Task 0020-polimento-pos-banca-codigo — Polimento pós-banca: projeção, correlação, alertas e carga

tipo: chore
branch: chore/0020-polimento-pos-banca-codigo
estado: em andamento
worktree: .claude/worktrees/chore-0020-polimento-pos-banca-codigo
responsavel: jhenriquecosta@hotmail.com
issue: #51

Origem: avaliação de `.tmp/pendencias-revisao-banca.md` (itens 2 a 5). A parte documental (itens 1 e 6) fica numa task docs separada.

## Critérios de aceitação

- [x] Um movimento que chega atrasado para o dia D propaga o efeito para o `saldo_acumulado` dos dias posteriores já abertos, na mesma transação; um teste de integração cobre D+1 projetado antes de D — `ConsolidadoTests.Movimento_que_chega_atrasado_propaga_o_efeito_para_o_acumulado_dos_dias_posteriores` (vermelho antes, verde depois)
- [x] O ADR-0005 registra que chegada tardia não é lançamento retroativo e por que a propagação existe — ADR-0005 v1.2, parágrafo *Chegada tardia não é lançamento retroativo*
- [x] O log de requisição HTTP traz `request_id` e `correlation_id` (a correlação vem antes do `UseSerilogRequestLogging`) — `CashflowWeb.cs`; **sem teste automatizado**: exigiria um sink de teste do Serilog
- [x] `CashflowServiceDown` cobre o job `background-server` — `cashflow-alerts.yml:66`
- [x] `RecordLag` consulta o broker no máximo uma vez a cada 5 s por partição — `KafkaReceiver.RecordLag`, com o último instante por `TopicPartition`
- [x] `./start.sh load` roda o k6 na rede do compose, com imagem de versão fixa, e o script de carga obtém o token do próprio consolidado — `./start.sh load` em 23/11: 3002 requisições, 0,00% de falha, p95 2,87 ms, `grafana/k6:2.4.0`
- [x] Build Release sem avisos, `dotnet format` limpo e a suíte completa verde — 0 avisos, format exit 0, **151/151** com `dotnet test --solution Cashflow.slnx -c Release --no-build`

## Exceção de portão

- **verify (`--no-verify`)**: o `verify_all.sh:280` do arsenal roda `dotnet test --no-build -q`, que o xunit.v3/MTP recusa (exit 5) e o portão lê como vermelho. A suíte real roda sem `-q`: build Release com 0 avisos, `dotnet format` limpo e 151/151 testes aprovados em 23/11/2026. O conserto é no `ultron-skills`, fora desta task.
