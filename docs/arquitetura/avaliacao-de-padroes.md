# Avaliação de padrões: onde Options e Strategy reduzem a dívida técnica

> Status: **parcialmente implementada**. O item 2 (Strategy do JWT) entrou na task 0019 e o item 5 (contrato do tipo) na task 0021; os itens 1, 3 e 4 seguem como proposta. A decisão fica registrada no [ADR-0012](../ultron/agents/adr/0012-options-validados-e-estrategias-nos-pontos-de-variacao.md).

## O critério

Um padrão reduz a dívida técnica quando responde a uma **variação real**: algo que já muda entre ambientes, ou que a arquitetura-alvo já diz que vai mudar. Aplicado onde nada varia, ele aumenta a dívida, porque acrescenta indireção que alguém precisa entender, testar e manter sem retorno nenhum.

Cada candidato abaixo passou por quatro perguntas:

1. **A variação existe?** No código de hoje, nos requisitos ou nos documentos do alvo, e não numa hipótese.
2. **Quanto custa mudar agora, e quanto custará depois?** Se o custo cresce com o tempo, vale antecipar.
3. **O padrão tira código ou põe código?** Trocar código escrito à mão por um mecanismo padrão do .NET reduz a dívida; criar uma abstração própria aumenta.
4. **Fica mais fácil ou mais difícil de testar?**

## Resumo

| # | Candidato | Variação | Decisão | Prioridade |
|---|---|---|---|---|
| 1 | **Options pattern** (settings tipados e validados na subida) | configuração por ambiente; segredos no Secrets Manager no alvo | **aplicar** | 1ª, é a base das outras |
| 2 | **Strategy na validação do JWT** (chave de dev × Cognito/JWKS) | requisito RF-7 | **implementado** (task 0019) | — |
| 3 | **Strategy de resiliência** (Polly no lugar dos laços de retry escritos à mão) | duas políticas diferentes, sem jitter, com números fixos no código | **aplicar** | 3ª |
| 4 | **Strategy do broker** (Kafka × SQS FIFO) | alternativa de custo documentada | **não implementar agora**; tornar explícitos o ponto de troca e o contrato | 4ª, pequena |
| 5 | Texto do contrato (`"credito"`) duplicado nos dois módulos | o contrato pode ganhar tipos | **implementado** (task 0021) | — |
| 6 | Polimorfismo no `TipoLancamento` | nenhuma: crédito e débito são o domínio | **não aplicar** | — |
| 7 | Resultado que gera a própria resposta HTTP | nenhuma | **não aplicar** | — |
| 8 | Strategy do dia de negócio (fuso por comerciante) | fora do escopo (requisito fixa São Paulo) | **não aplicar** | — |
| 9 | Biblioteca de mediador (MediatR) | nenhuma; o `Dispatcher` já cumpre o papel | **não aplicar** | — |

---

## 1. Options pattern: aplicar

### O que o código mostra

- **A mesma chave é lida por texto em vários lugares.** `Auth:SigningKey`, `Auth:Issuer` e `Auth:Audience` são lidas por string em `CashflowAuth` e de novo em `DevToken`, e os valores padrão estão num dicionário à parte (`CashflowDefaults`). Renomear uma chave exige mexer em três arquivos, e o compilador não aponta o que faltou.
- **A seção de cada módulo é lida duas vezes.** O host faz `configuration.Bind(appSettings)` para decidir quais módulos ligar e depois `GetSection("Modules:Lancamentos").Bind(opt)` para configurar o módulo. O nome da seção é um literal repetido, embora o módulo já tenha a constante `LancamentosModuleOptions.Key`.
- **A validação está espalhada e escrita à mão.** Há `Required(...)` e `TopicFor(...)` no registro do Kafka e um `if` no data source. A chave HMAC com menos de 32 bytes não é validada: o erro só aparece na primeira requisição autenticada.
- **Os padrões estão em três lugares.** O dicionário `CashflowDefaults`, a constante `DefaultBusinessTimeZone` e o valor inicial `Provider = "Kafka"`.
- **Os parâmetros operacionais estão fixos no código.** Lote do outbox (100), esperas do worker (500 ms e 1 s), janela de reserva abandonada (30 s) e tentativas do consumidor (1, 2 e 4 s). Ajustá-los em produção exige uma nova versão.

### A proposta

Uma classe de opções por seção, registrada pelo mecanismo padrão do .NET:

```csharp
services.AddOptions<AuthOptions>()
    .BindConfiguration(AuthOptions.Section)
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

| Seção | Classe | Validação na subida |
|---|---|---|
| `Auth` | `AuthOptions` | `Issuer` e `Audience` obrigatórios; `Authority` **ou** `SigningKey` (≥ 32 bytes), nunca os dois |
| `TimeZone` | `BusinessTimeOptions` | o fuso existe no sistema |
| `MessageBroker` | `MessageBrokerOptions` | provider conhecido; bootstrap, tópicos e DLT por tipo de mensagem |
| `Modules:<X>` | `LancamentosModuleOptions`, `ConsolidadoModuleOptions` | connection string obrigatória quando o módulo está ligado |
| `Outbox` | `OutboxOptions` | lote entre 1 e 1000; esperas positivas |
| `Idempotency` | `IdempotencyOptions` | janela de reserva abandonada > 0 |

Os valores padrão passam a morar no inicializador de cada propriedade, num lugar só. O dicionário `CashflowDefaults` deixa de existir.

### Por que reduz a dívida técnica

- **O deploy mal configurado falha na subida, não às 3h da manhã na primeira requisição.** O `ValidateOnStart` transforma a configuração errada em container que não fica saudável, e o orquestrador não manda tráfego para ele.
- **O Secrets Manager do alvo passa a ser só mais uma fonte de configuração.** Ninguém lê segredo por texto no meio do código, e trocar a origem do segredo não muda nenhuma classe.
- **Renomear uma chave é mudar um lugar só.** O nome da seção é uma constante da própria classe de opções.
- **Tira código.** O `Required`, o `TopicFor` e as exceções escritas à mão viram atributos de validação.
- **Segue a referência.** O Practical.CleanArchitecture usa `ConfigurationOptions` e `IOptions<T>`, o que mantém a aderência cobrada no ADR-0009.

### Custo e risco

Médio. Mexe nos três `Program.cs`, na `Infrastructure` e no registro dos módulos. O comportamento não muda, e a suíte atual (77 testes) cobre o caminho. A fábrica de testes passa a ter um lugar tipado para sobrescrever configuração.

---

## 2. Strategy na validação do JWT: implementado

> **Implementado na task 0019.** `Auth:Authority` liga a validação pelos metadados OIDC e pelo JWKS do emissor; sem ela, vale a chave de desenvolvimento, e só então o `/dev/token` existe. Prova: um teste com emissor OIDC falso (metadata + JWKS) aceita o token da chave publicada e recusa, com 401, o de outra chave. O texto abaixo é a avaliação que levou à decisão.

### O que o código mostrava

O RF-7 diz: *"O JWT é validado pelas chaves públicas (JWKS)"*, e o `seguranca-integracao.md` diz que o alvo é o Cognito. Antes da task 0019, o `CashflowAuth` só sabia validar com a **chave simétrica de desenvolvimento**, e levar o sistema ao Cognito exigiria reescrever a autenticação.

### A proposta

Duas estratégias de validação, escolhidas pela configuração, atrás da mesma configuração do `JwtBearer`:

| Estratégia | Quando | Como valida |
|---|---|---|
| `OidcAuthority` (alvo) | `Auth:Authority` preenchido | metadados OIDC e JWKS do emissor; a rotação de chaves é automática |
| `ChaveDeDesenvolvimento` (local) | `Auth:SigningKey` preenchido | HMAC com a chave local, como hoje |

As duas aplicam as mesmas regras: emissor, audiência, validade sem tolerância de relógio, escopo e `comercianteId`. Só muda **de onde vem a chave**. O `/dev/token` passa a existir só com a estratégia de desenvolvimento ativa, em vez de depender só de `IsDevelopment()`. Assim, um ambiente com Cognito nunca emite token próprio, mesmo configurado por engano como `Development`.

A implementação usa o ponto de extensão do próprio ASP.NET Core (`IConfigureNamedOptions<JwtBearerOptions>`), e não uma interface própria. A estratégia é configuração, e o handler continua sendo o do framework.

### Por que reduz a dívida técnica

- Fecha uma lacuna em relação a um requisito escrito, e não a uma hipótese.
- Ir para o Cognito vira configuração (`Auth__Authority=https://cognito-idp...`), sem código.
- Os testes continuam com a chave local: a estratégia do alvo é provada por um teste com o metadata de um emissor falso.

### Custo e risco

Pequeno a médio. Depende da opção 1 (`AuthOptions`).

---

## 3. Strategy de resiliência: aplicar, com Polly

### O que o código mostra

Há dois laços de nova tentativa escritos à mão, com políticas diferentes e sem justificativa para a diferença:

| Onde | Política | Problema |
|---|---|---|
| `DatabaseRetryDecoratorBase` | 3 tentativas, espera linear de 100 ms × n | sem jitter: quando o banco volta, todas as réplicas tentam no mesmo instante |
| `KafkaReceiver` | 4 tentativas, esperas fixas de 1, 2 e 4 s | números fixos no código; sem jitter; sem métrica por tentativa |

### A proposta

Trocar os dois laços por pipelines do `Microsoft.Extensions.Resilience` (Polly v8), registrados por nome e configurados pelas opções:

- `database`: retry exponencial com jitter, só em `DbException.IsTransient`.
- `consumer`: retry exponencial com jitter e, depois de esgotado, o mesmo desvio para a DLT de hoje.

O Polly é o Strategy pattern padronizado pelo ecossistema .NET. Retry, timeout e circuit breaker são estratégias que se compõem num pipeline e emitem métrica e log de cada tentativa sem código extra.

### Por que reduz a dívida técnica

- **Tira código próprio.** Dois laços que hoje precisam de teste e manutenção viram configuração de uma biblioteca mantida pela Microsoft.
- **Jitter** evita o efeito manada na volta do banco.
- **Um circuit breaker no consumidor**, se um dia for preciso, passa a ser uma linha a mais, e não um novo laço.
- **Métricas de cada tentativa** passam a chegar ao Prometheus, o que ajuda a investigar a DLT.

### Custo e risco

Médio. O retry do consumidor está entrelaçado com o desvio para a DLT, e o teste AC7 (mensagem envenenada vai para a DLT) é a rede de proteção. Vale fazer depois da opção 1, porque os parâmetros vêm das opções.

---

## 4. Strategy do broker: não implementar agora; deixar a costura explícita

### O que o código mostra

A troca de broker já está isolada: os módulos falam só com `IMessageBus`, e o Kafka é um adaptador de `IMessageSender`/`IMessageReceiver` (Ports and Adapters, que é o Strategy no nível da arquitetura). O único ponto ruim é a escolha, feita por `if (!options.UsesKafka()) throw`, comparando texto, e repetida em dois métodos.

O `custos.md` registra o **SQS FIFO como alternativa que corta pela metade o custo do MSK**. A variação é documentada, então não é hipótese.

### Por que não implementar o SQS agora

- **Substituir o broker não é de graça, e um padrão não muda isso.** O SQS FIFO garante ordem por `MessageGroupId` e deduplicação, mas **não tem replay**. A projeção do consolidado deixaria de poder ser reconstruída a partir do log, que é uma das três razões da escolha do Kafka (requisitos, "Kafka em vez de fila simples"). Um adaptador SQS compilaria e passaria nos testes, e mesmo assim quebraria uma garantia da arquitetura. É exatamente o tipo de dívida que um Strategy "pronto" esconderia.
- **Uma implementação só não justifica um registro de provedores.** Com um único broker, o registro seria indireção sem retorno.

### O que fazer agora (pequeno)

- `Provider` vira um `enum` validado pelas opções, e a escolha fica num `switch` único, com o erro dizendo os valores aceitos.
- O contrato das portas ganha, na documentação, as garantias que todo adaptador precisa honrar: **ordem por chave, at-least-once e retenção para replay**. Quem for escrever um adaptador SQS encontra ali, antes de começar, a garantia que ele não cumpre.

---

## 5. O texto do contrato duplicado nos dois módulos: implementado

> **Implementado na task 0021.** `Cashflow.Contracts.TiposDeLancamento` tem as duas constantes, usadas pelos dois módulos, e o `MovimentoDoDia` recusa um tipo fora do contrato na construção. O texto abaixo é a avaliação que levou à decisão.

`"credito"` e `"debito"` aparecem como texto literal em `TipoLancamento` (Lançamentos) e em `MovimentoDoDia` (Consolidado). São o **contrato** do evento, mas não estão no `Cashflow.Contracts`. Um terceiro tipo, ou uma mudança de grafia, quebraria o consolidado em silêncio: o `MovimentoDoDia` trataria qualquer coisa diferente de `"credito"` como débito.

**Correção:** `Cashflow.Contracts.TiposDeLancamento` com as duas constantes, usada pelos dois lados. O `MovimentoDoDia` passa a recusar um tipo desconhecido, em vez de tratá-lo como débito, e a mensagem vai para a DLT. Não é padrão de projeto: é tratar o contrato como contrato.

---

## 6 a 9. Onde não aplicar, e por quê

| Candidato | Por que não |
|---|---|
| **Polimorfismo no `TipoLancamento`** (uma classe por tipo, com `EfeitoNoSaldo` e `Oposto`) | Crédito e débito não são uma família aberta: são a definição do livro-caixa. Duas classes e uma hierarquia para um `if` de uma linha são mais código, não menos. O `enum` com duas extensões diz tudo. |
| **Resultado que gera a própria resposta HTTP** (cada `ResultadoDoLancamento` devolve o seu `IResult`) | Levaria o HTTP para dentro do módulo de domínio. O `switch` na borda (`ParaRespostaHttp`) é o lugar certo da tradução, e o `default → 500` denuncia um caso esquecido. |
| **Strategy do dia de negócio** (fuso por comerciante) | O requisito fixa `America/Sao_Paulo`, e o comerciante multifuso está fora do escopo. O fuso já é configuração (`TimeZone:Business`); uma estratégia por comerciante seria uma resposta a uma pergunta que ninguém fez. |
| **MediatR no lugar do `Dispatcher`** | O `Dispatcher` faz o que é preciso em 70 linhas, com decorators por atributo e cobertura do ArchTests. Trocar acrescenta uma dependência externa e uma licença a acompanhar (o MediatR passou a ter licença comercial em 2025), sem ganho funcional. |

---

## Ordem proposta e dependências

```
1. Options pattern ──┬──▶ 2. Strategy do JWT (JWKS)
                     ├──▶ 3. Resiliência com Polly
                     └──▶ 4. Escolha do broker por enum + 5. contrato tipado
```

**Estado:** a 2 entrou na task 0019 sem esperar a 1, lendo `Auth:Authority` e `Auth:SigningKey` direto da configuração; a 5 entrou na task 0021. Seguem propostas a 1, a 3 e a 4.

A opção 1 é a base: as outras leem parâmetros que ela tipa e valida. A 2 tem o maior retorno, porque fecha uma lacuna em relação a um requisito escrito. A 3 tira código próprio. A 4 e a 5 são pequenas e podem ir juntas.

**Momento:** as opções 1 e 2 mexem nos `Program.cs` e na `CashflowAuth`, que a branch do frontend (task 0008) também altera. Implementar depois que o frontend entrar na `develop` evita um terceiro conflito no mesmo arquivo.
