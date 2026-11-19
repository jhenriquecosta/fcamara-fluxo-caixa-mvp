# Cashflow.WebAPI

> O host HTTP: um binário que liga os módulos que o deploy pedir.

## Por que existe

O requisito diz que o serviço de lançamentos não pode cair quando o consolidado cair. A resposta comum seria dois projetos de API. A referência resolve com **um** host e módulos ligáveis: o mesmo binário é implantado duas vezes, como `lancamentos-api` (`Modules:Consolidado:Enabled=false`) e como `consolidado-api` (`Modules:Lancamentos:Enabled=false`). O isolamento de falha fica no deploy, sem código duplicado.

## Finalidade

- Monta a infraestrutura comum (`AddCashflowInfrastructure`, `UseCashflowWeb`, `MapCashflowEndpoints`).
- Liga cada módulo habilitado: o registro (`AddXModule`) e as rotas (`MapXEndpoints`).
- **Não roda trabalho de fundo**: nem o outbox nem o consumidor. Isso é do BackgroundServer, e um teste garante (`WebAPI_nao_sobe_trabalho_de_fundo...`).

`WebApiEntryPoint` é só o marcador que o `WebApplicationFactory` dos testes usa para achar este host.

## Estilo arquitetural e padrões

- **Composition Root**: o único lugar onde módulos e infraestrutura se encontram.
- **Modular Monolith** com **deploy por feature flag** de módulo (`Modules:<X>:Enabled`).
- **Minimal APIs**: as rotas são dos módulos; o host não declara rota de negócio.

## Configuração

| Chave | O que é |
|---|---|
| `ServiceName` | o nome nos logs e nas métricas |
| `Modules:Lancamentos:Enabled`, `Modules:Consolidado:Enabled` | quais módulos ligam |
| `Modules:<X>:ConnectionStrings:Default` | o banco de cada módulo |
| `Auth:*`, `TimeZone:Business`, `Serilog:*` | padrões em `CashflowDefaults`, sobrescritos por ambiente |

## Dependências

Pode referenciar tudo; é o topo. Nenhum outro projeto de `src/` pode referenciá-lo (ArchTests).

## Decisões

- [ADR-0001](../../docs/ultron/agents/adr/0001-dois-servicos-outbox-projecao.md): o isolamento de falha.
- [ADR-0009](../../docs/ultron/agents/adr/0009-monolito-modular.md): um host, dois deploys.
