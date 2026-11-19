# Descoberta — Experiência de demonstração (task 0011)

**Data:** 2026-11-19 · **Modo:** autônomo · **Issue:** #27 · **Base:** `feature/0008-frontend-blazor`

## O pedido

> "Vamos criar também uma UI do tipo terminal, pra experiência ficar mais ampla. Use o ultron-preview para criar o start.sh, onde os testes e o fluxo serão expostos ao usuário. [...] o usuário tenha acesso aos outros serviços: consultar os logs, consultar Prometheus, consultar ELK. O front é a experiência que demonstra que toda a infra está funcionando." — Operador

## Gaps e decisões

| # | Gap | Decisão | Por quê |
|---|---|---|---|
| G1 | Onde vivem os clientes de Prometheus, Elasticsearch e saúde | `Cashflow.Blazor.Modules/Infraestrutura/Services`, ao lado de `Lancamentos` e `Consolidado` | Um cliente, duas UIs: a web e o terminal consomem as mesmas classes. Segue o molde "pasta por módulo" da referência |
| G2 | Tecnologia da UI de terminal | `Cashflow.Terminal`, console .NET com **Spectre.Console** | Mesma stack, mesmos clientes HTTP. Tabelas, painéis e menus prontos. Testável com `Spectre.Console.Testing` |
| G3 | Autenticação do terminal | O terminal é a persona **integrador**: pede ao `/dev/token` (só em Development) um token com os escopos do perfil escolhido | No alvo, integrador usa client credentials (ADR-0010). Localmente, o emissor de dev é o equivalente honesto, e o terminal não precisa duplicar o cadastro de usuários do front |
| G4 | Painel de infraestrutura em produção | Liga só com `Infraestrutura:Enabled=true` (o compose liga). Fica atrás de login, perfil consulta ou operador | É superfície de demonstração. Em produção, quem olha a operação é o Grafana/CloudWatch com o IAM próprio |
| G5 | O que o painel mostra | Saúde de cada serviço (APIs, background, Prometheus, Elasticsearch, Kibana, Grafana) · `up` por job · tráfego, p95 e taxa de erro por serviço · idade do outbox · lag do consumidor · DLT · lançamentos por resultado · últimos logs (sem `/metrics` e `/health`) · atalhos para Grafana, Kibana e Prometheus | Os sinais são os mesmos das regras de alerta (`observability/prometheus/rules`): o painel mostra o que o alarme vigia |
| G6 | Um serviço de observabilidade fora | Cada bloco degrada sozinho ("indisponível") | Mesmo princípio da falha isolada (ADR-0001) aplicado à superfície de demonstração |
| G7 | Demo ponta a ponta: no `start.sh` ou no terminal | Implementada **uma vez**, no terminal (`Cashflow.Terminal demo`), e chamada pelo `start.sh demo` | Bash com `curl`+`jq` duplicaria a lógica dos clientes. Uma implementação, dois pontos de entrada |
| G8 | `start.sh` | Padrão ultron-preview: bloco-manifesto com hash, compose como orquestrador (não gera outro), verificação antes de subir, health por serviço, `apply-elk.sh`, painel final. Subcomandos `up` (padrão), `test`, `demo`, `terminal`, `status`, `logs`, `down` | O compose já é a declaração da stack. O script só orquestra a experiência |
| G9 | Nome do projeto compose | `CASHFLOW_COMPOSE_PROJECT`, padrão `cashflow` | Permite subir isolado (`cashflow-web`) sem derrubar outra stack, como foi preciso na verificação da 0008 |
| G10 | Atualização do painel web | `PeriodicTimer` de 10 s enquanto a página está aberta | O Blazor Server já mantém o circuito, e polling curto basta para a demo |

## Fora de escopo

Alertas no painel (o Alertmanager não está no compose) · edição de dashboards · streaming de logs em tempo real · autenticação de Grafana e Kibana.
