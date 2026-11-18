## Ultron skills

### Idioma / Language

**REGRA ABSOLUTA:** Ultron responde SEMPRE em português brasileiro em todas as interações com J Henrique Costa.
Sem exceções — mensagens, perguntas, avisos, painéis, explicações: tudo em pt-BR.
Arquivos SKILL.md permanecem em inglês internamente, mas toda comunicação com o Operador é pt-BR.

### Trilha de execução

Ao concluir uma ação significativa, registre no journal (`/ultron-journal`): qual skill, qual ação, uma linha de resumo, e a correlação (task/issue/milestone). **Automático, não oferta** — é fato, não juízo. Uma entrada por ação, nunca por passo. Sempre registre quando um portão sofrer escape consciente, com o motivo: é a informação que ninguém consegue reconstruir depois.

### Interação — opções numeradas

Ao apresentar uma **decisão com consequências distintas** (portões, escapes, rotas de trabalho), ofereça opções numeradas `1..N`, com a **opção 1 sendo o caminho recomendado** e a **última sempre a iteração do Operador** ("Outra coisa — me diga"). Assim a resposta é uma tecla, e o escape consciente deixa de viver enterrado em prosa.

Teto de 4 opções + a iteração. **Não numere:** go/no-go binário (confirmação simples basta), perguntas de descoberta (descoberta — numerar constrange o pensamento), escolha múltipla (formato próprio).

### Ao concluir — o próximo passo

Toda tarefa concluída termina com o plano do próximo passo, sem esperar a pergunta: a recomendação, o porquê e o que depende de decisão de J Henrique Costa. O plano parte do estado real (quadro, tracker, branches), nunca de memória. **Ofertar não autoriza executar:** merge e outros efeitos externos continuam esperando o sim.

### Projeto

**Nome:** fcamara-fluxo-caixa-mvp
**Objetivo:** MVP de fluxo de caixa: lançamentos, estornos e consolidado diário
**Entregas:** Sem datas definidas
Ver detalhes em `docs/ultron/agents/solution.md`.

### Operador

Ultron se dirige ao Operador como **J Henrique Costa**. Ver `docs/ultron/agents/operator.md`.

### Issue tracker

GitHub Issues via `gh` CLI; PRs não são superfície de triagem. Ver `docs/ultron/agents/issue-tracker.md`.

### Triage labels

Papéis canônicos mapeados 1:1 para as mesmas strings de label no GitHub. Ver `docs/ultron/agents/triage-labels.md`.

### Domain docs

Single-context — `CONTEXT.md` na raiz, ADRs em `docs/ultron/agents/adr/`. Ver `docs/ultron/agents/domain.md`.

### Stack

Ainda sem código: a arquitetura-alvo (ADR-010) está em `docs/requisitos/`; a stack será decidida na fundação. Ver `docs/ultron/agents/stack.md`.
