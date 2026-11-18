# Arquitetura de negócio — fluxo de caixa do comerciante

Mapa de negócio da solução: domínios funcionais, capacidades, cadeia de valor, conceitos de informação, donos e sistemas. Cada linha tem **fonte** e **estado**; só linha `confirmada` alimenta o desenho e o código.

## Domínios funcionais (capacidades de nível 1)

| Domínio | O que o negócio faz | Capacidades de nível 2 |
|---|---|---|
| CAP-1 Gestão do fluxo de caixa | Registra débitos e créditos, estorna e apura o saldo do dia | CAP-1.1 a CAP-1.4 |
| CAP-2 Gestão de acesso e segurança | Autentica pessoas e integradores e protege a borda | CAP-2.1 a CAP-2.5 |
| CAP-3 Integração por eventos | Publica e consome eventos de lançamento | CAP-3.1 e CAP-3.2 |
| CAP-4 Observabilidade e operação | Monitora e rastreia o fluxo | CAP-4.1 e CAP-4.2 |

## Cadeia de valor

**VS-1 Controle do fluxo de caixa diário:** registrar lançamento → estornar lançamento → consolidar o dia → consultar saldo.

## Arquivos

| Arquivo | Conteúdo |
|---|---|
| `capabilities.md` | Capacidades com ID estável, fonte e estado |
| `value-streams.md` | Estágios da cadeia de valor e as capacidades que os sustentam |
| `information.md` | Conceitos de informação e as capacidades que os criam ou consomem |
| `organization.md` | Dono de cada capacidade confirmada |
| `systems.md` | Sistemas que sustentam as capacidades |

## Como o mapa é usado

- A **cadeia de valor** semeia a descoberta de domínio (event storming) com os estágios que este software apoia.
- As **capacidades** informam as fronteiras de contexto do desenho.
- O `docs/ultron/agents/solution.md` declara, em `## Arquitetura de negócio`, quais capacidades este software realiza.
- O lint do blueprint (`business_lint.py`) valida IDs, fontes, estados e vínculos a cada mudança.
