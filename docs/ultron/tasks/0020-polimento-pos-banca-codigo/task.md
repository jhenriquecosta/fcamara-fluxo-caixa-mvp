# Task 0020-polimento-pos-banca-codigo — Polimento pós-banca: projeção, correlação, alertas e carga

tipo: chore
branch: chore/0020-polimento-pos-banca-codigo
estado: em andamento
worktree: .claude/worktrees/chore-0020-polimento-pos-banca-codigo
responsavel: jhenriquecosta@hotmail.com
next_skill: ultron-coder
issue: #51

Origem: avaliação de `.tmp/pendencias-revisao-banca.md` (itens 2 a 5). A parte documental (itens 1 e 6) fica numa task docs separada.

## Critérios de aceitação

- [ ] Um movimento que chega atrasado para o dia D propaga o efeito para o `saldo_acumulado` dos dias posteriores já abertos, na mesma transação; um teste de integração cobre D+1 projetado antes de D
- [ ] O ADR-0005 registra que chegada tardia não é lançamento retroativo e por que a propagação existe
- [ ] O log de requisição HTTP traz `request_id` e `correlation_id` (a correlação vem antes do `UseSerilogRequestLogging`)
- [ ] `CashflowServiceDown` cobre o job `background-server`
- [ ] `RecordLag` consulta o broker no máximo uma vez a cada 5 s por partição
- [ ] `./start.sh load` roda o k6 na rede do compose, com imagem de versão fixa, e o script de carga obtém o token do próprio consolidado
- [ ] Build Release sem avisos, `dotnet format` limpo e a suíte completa verde
