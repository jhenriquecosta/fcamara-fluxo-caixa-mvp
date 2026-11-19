# Cashflow.BlazorServerSide.IntegrationTests

> O frontend exercitado como o navegador o vê: HTTP de verdade, contra a API real em memória.

## Por que existe

O frontend é mais um cliente da API (ADR-0010), e o que importa nele não é o componente isolado: é a sessão, o cookie, a policy e a chamada que chega à API. Aqui o host real do front sobe com a `WebApiFactory` no lugar do servidor HTTP, e o teste dirige o mesmo caminho que o navegador usaria.

## O que cobre

| Área | O que prova |
|---|---|
| Login | rota protegida sem sessão redireciona; credencial inválida não cria cookie; `ReturnUrl` só local (open redirect recusado) |
| Sessão | cookie `HttpOnly`/`SameSite=Lax`; o token de acesso não aparece no HTML nem no cookie |
| Perfis | `operador` lança e estorna; `consulta` não vê as ações e `/lancamentos/novo` responde acesso negado; a API responde 403 mesmo se a tela chamar |
| Telas | lançar, listar, estornar e consolidado contra a API real; erro da API aparece na tela sem derrubar o circuito |
| Infraestrutura | o painel lê saúde, Prometheus e Elasticsearch reais e degrada quando a fonte cai |
| Saúde | `/health/live` e `/health/ready` respondem no host do front |

## Princípio

Os testes sobem o **mesmo `Program`** do front e o mesmo da API; a única diferença é a configuração — usuários de teste, tokens assinados com a chave dos testes e o cliente HTTP apontado para o servidor em memória.
