# Cashflow.Blazor.Modules

> As telas e os clientes HTTP de cada módulo, num RCL que só conhece o contrato da API.

## Por que existe

Duas UIs — o navegador e o terminal — precisam das mesmas consultas e dos mesmos modelos. Se cada host trouxesse a sua cópia, as duas divergiriam no primeiro ajuste de contrato. Aqui a lógica de consulta mora uma vez só, e os hosts (Blazor Server e terminal) apenas a consomem.

## Finalidade

| Área | O que tem |
|---|---|
| `Core` | `HttpService` (bearer via `ITokenManager`, `ProblemDetails` traduzido em `ApiException`), `DiaDeNegocio`, `Formatacao`, `Perfis`/`Policies` |
| `Lancamentos` | `LancamentoService` e as telas de lançar, listar e estornar |
| `Consolidado` | `ConsolidadoService` e a tela do saldo |
| `Infraestrutura` | clientes de saúde, Prometheus e Elasticsearch, e o painel do front (ADR-0011) |

## Estilo arquitetural e padrões

- **Cliente por módulo**, como o contrato HTTP o desenha: um serviço por recurso, sem estado compartilhado.
- **BFF**: o componente nunca vê o token; toda chamada sai do servidor, com o bearer anexado no `HttpService`.
- **Uma classe por responsabilidade de tela**, sem código de negócio: a regra é da API.

## Dependências

Só o contrato HTTP: não referencia `Cashflow.Modules.*`, `Domain`, `Application`, `Contracts` nem `Infrastructure` — um ArchTest garante. É referenciado pelos dois hosts de UI (`Cashflow.BlazorServerSide` e `Cashflow.Terminal`).

## Decisões

- [ADR-0010](../../docs/ultron/agents/adr/0010-frontend-blazor-bff.md): o frontend como cliente, com o token no servidor.
- [ADR-0011](../../docs/ultron/agents/adr/0011-superficie-de-demonstracao.md): um cliente, duas UIs.
