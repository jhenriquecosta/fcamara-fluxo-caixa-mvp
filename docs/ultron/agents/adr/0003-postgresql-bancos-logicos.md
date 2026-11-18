# 0003. PostgreSQL com dois bancos lógicos na mesma instância

| Campo | Conteúdo |
|---|---|
| **Documento** | ADR-0003 · Architecture Decision Record |
| **Projeto** | fcamara-fluxo-caixa-mvp |
| **Autor** | ULTRON · skill ultron-adr — Operador: J Henrique Costa |
| **Data** | 2026-11-18 |
| **Versão do documento** | 1.1 |
| **Status** | accepted |
| **Rastreabilidade** | Desafio Carrefour (via FCamara) · PRD-0001 · Research #0001 · ADR-010 (desenho-base) |

> **Uma linha:** Um PostgreSQL Multi-AZ com dois bancos lógicos e usuários próprios por serviço — a separação protege os dados, não a instância.

## Contexto

Cada serviço precisa ser dono dos seus dados: o consolidado não pode escrever no banco de lançamentos, e o modelo de escrita não lê a projeção. O RNF do desafio pede que uma falha no consolidado não derrube os lançamentos. Há uma tensão de custo e operação: separar fisicamente os bancos aumenta a resiliência e dobra o custo e o esforço de operação no MVP. O ADR-010 escolheu uma instância Multi-AZ com dois bancos lógicos; a pesquisa confirma que o RDS `db.m7g.large` Multi-AZ fica em torno de **US$ 0,337/h** (≈ US$ 246/mês), sem contar armazenamento gp3 Multi-AZ (≈ US$ 0,23/GB-mês) (Research #0001, pergunta 10).

## Decisão

Vamos usar **PostgreSQL**, com **dois bancos lógicos** — `lancamentos` e `consolidado` — e **um usuário próprio por serviço**, na **mesma instância Multi-AZ** no alvo. No docker compose local, um único container PostgreSQL com os dois bancos. O isolamento entre os serviços é de **aplicação**; a instância é um domínio de falha compartilhado.

## Alternativas consideradas

- **Um banco e um schema compartilhados** — rejeitado: acopla os modelos, permite escrita cruzada e apaga a fronteira de propriedade que o desenho quer demonstrar.
- **Duas instâncias separadas (uma por serviço)** — rejeitado para o MVP: atende ao RNF também no nível de infraestrutura, mas dobra o custo e a operação; fica registrado como a evolução do alvo quando a leitura do RNF exigir isolamento físico.
- **Banco por serviço em clusters distintos com replicação** — overkill para dois serviços e dois dias de entrega; rejeitado.

## Consequências

- **Positivas:** fronteira de dados clara por usuário e banco; operação e custo menores; Multi-AZ cobre a falha da instância; cabe no MVP local sem diferença de contrato.
- **Negativas / trade-offs aceitos:** se a instância cair, os dois serviços param — a separação é lógica, não física; há risco de vizinho barulhento no mesmo servidor; o RNF-1 é atendido no nível da aplicação, não no da infraestrutura. Essa é a decisão que a banca pode contestar; a resposta é o trade-off explícito e a evolução registrada.
- **Desdobramentos:** monitorar os dois bancos e os pools separadamente; backup e restauração testados; documentar a separação de instâncias como evolução no documento de transição.

---

**Proveniência.** Desafio Carrefour — Requisitos não funcionais; ADR-010 (nota de separação lógica); PRD-0001 (RNF-1); Research #0001, pergunta 10 (preço de RDS Multi-AZ).

**Histórico de versões.**

| Versão | Data | Autor | Mudança |
|---|---|---|---|
| 1.0 | 2026-11-18 | ULTRON · ultron-adr | Criação |
| 1.1 | 2026-11-18 | ULTRON · ultron-adr | Operador aceita a decisão — status accepted |

**Rastreabilidade.** Desafio Carrefour (via FCamara) › repositório `fcamara-fluxo-caixa-mvp` › PRD-0001 › ADR-0003.

<sub>Gerado por ULTRON v2.1.1 · skill ultron-adr · 2026-11-18T20:27:21Z · em conformidade com a Convenção de Documento ULTRON. Documento versionado em git — a proveniência viaja com o projeto.</sub>
