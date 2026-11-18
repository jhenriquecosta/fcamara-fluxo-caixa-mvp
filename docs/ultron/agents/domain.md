# Domain Docs

Como as skills do Ultron devem consumir a documentação de domínio deste repositório ao explorar o código.

## Antes de explorar, leia

- **`CONTEXT.md`** na raiz do repositório, ou
- **`CONTEXT-MAP.md`** na raiz se existir — ele aponta para um `CONTEXT.md` por contexto delimitado. Leia cada um relevante para o tema.
- **`docs/ultron/agents/adr/`** — leia os ADRs que tocam a área em que você vai trabalhar. Em repositórios multi-contexto, verifique também `src/<contexto>/docs/ultron/adr/` para decisões do contexto.

Se algum desses arquivos não existir, **siga em silêncio**. Não aponte a ausência; não sugira criá-los de antemão. `ultron-glossary` e `ultron-ddd` os criam preguiçosamente quando termos ou decisões de fato forem resolvidos.

## Estrutura de arquivos

Repositório de contexto único (a maioria):

```
/
├── CONTEXT.md
├── docs/ultron/agents/adr/
│   ├── 0001-event-sourced-orders.md
│   └── 0002-postgres-for-write-model.md
└── src/
```

Repositório multi-contexto (presença de `CONTEXT-MAP.md` na raiz) — tipicamente um contexto delimitado por módulo de topo:

```
/
├── CONTEXT-MAP.md
├── docs/ultron/agents/adr/               ← decisões do sistema inteiro
└── src/
    ├── Ordering/
    │   ├── CONTEXT.md
    │   └── docs/ultron/adr/                  ← decisões específicas do contexto
    └── Billing/
        ├── CONTEXT.md
        └── docs/ultron/adr/
```

**Este repositório:** single-context.

## Use o vocabulário do glossário

Quando sua saída nomear um conceito de domínio (título de issue, proposta de refatoração, hipótese, nome de teste), use o termo como definido no `CONTEXT.md`. Não desvie para sinônimos que o glossário evita.

Se o conceito ainda não está no glossário, isso é um sinal — ou você está inventando linguagem que o projeto não usa (reconsidere), ou há uma lacuna real (anote para o `ultron-glossary` ou `ultron-ddd`).

## Aponte conflitos de ADR

Se sua saída contradiz um ADR existente, exponha isso explicitamente em vez de sobrescrever em silêncio:

> _Contradiz o ADR-0007 (event-sourced orders) — mas vale reabrir porque…_
