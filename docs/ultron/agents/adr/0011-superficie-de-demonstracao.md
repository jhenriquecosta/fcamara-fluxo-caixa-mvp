# ADR-0011 — Superfície de demonstração: um cliente, duas UIs e o start.sh

**Status:** accepted
**Data:** 2026-11-19
**Decisores:** J Henrique Costa (Operador) · ULTRON
**Relacionados:** ADR-0010 (frontend BFF), ADR-0001 (falha isolada) · PRD-0004 · Research #0003

## Contexto

A banca precisa ver que a solução funciona por inteiro: API, mensageria, projeção, métricas e logs. O frontend (0008) mostra o negócio. Faltava mostrar a infraestrutura e oferecer um caminho de um comando só.

## Decisão

1. **Clientes de infraestrutura no `Cashflow.Blazor.Modules/Infraestrutura`:** saúde (HTTP por serviço), Prometheus (API de consulta instantânea) e Elasticsearch (busca dos últimos logs). São HTTP puro, sem token, porque localmente esses serviços não têm autenticação.
2. **UI de terminal `Cashflow.Terminal`** (Spectre.Console), cliente da API como o front. Usa os mesmos serviços do RCL. Autentica como **integrador** pelo `/dev/token`, que só existe em Development, com os escopos do perfil escolhido.
3. **Painel web `/infraestrutura`**, ligado só por `Infraestrutura:Enabled`.
4. **`start.sh`** no padrão ultron-preview: o compose é o orquestrador, e o script conduz a experiência (verifica, sobe, espera a saúde, configura o ELK, demonstra). A demo ponta a ponta tem uma implementação só, no terminal.

## Alternativas rejeitadas

| Alternativa | Por que não |
|---|---|
| Embutir Grafana/Kibana no front via iframe | Exige desligar `X-Frame-Options` dos dois e configurar autenticação anônima. O painel mostra os sinais e dá o link |
| Demo em bash com `curl` + `jq` | Duplica a lógica dos clientes e quebra em silêncio quando o contrato muda |
| TUI com login próprio de usuário e senha | Duplica o cadastro do front. Terminal é integrador, não pessoa |

## Consequências

- **Positivas:** um comando prova a solução inteira. Os sinais do painel são os mesmos dos alertas. O contrato da API ganha um terceiro consumidor que os testes exercitam.
- **Negativas:** o painel expõe logs a quem tem sessão no front, o que é aceitável só com `Infraestrutura:Enabled` em ambiente de demonstração. O terminal depende do `/dev/token` e não roda contra um ambiente sem Development.
