# Cashflow.CrossCuttingConcerns

> Utilitários que qualquer projeto pode usar sem arrastar dependência nenhuma.

## Por que existe

Algumas necessidades atravessam todas as camadas: saber que horas são, pseudonimizar um dado antes de logar, sinalizar configuração inválida. Se cada camada resolvesse do seu jeito, o domínio leria `DateTime.UtcNow` direto (e ficaria impossível testar o fuso de São Paulo) e cada módulo inventaria seu hash para log. Este projeto é o lugar único dessas respostas.

## Finalidade

- **`DateTimes/IDateTimeProvider`**: o relógio atrás de uma interface. O domínio pergunta a hora aqui; o teste fixa o instante.
- **`Security/PseudonimoDeLog`**: o pseudônimo estável de comerciante e de chave de idempotência para os logs (LGPD), e a impressão digital SHA-256 de um texto.
- **`Exceptions/CashflowConfigurationException`**: configuração obrigatória ausente, lançada na subida do host.
- **`ExtensionMethods/TypeExtensions`**: a reflexão que a montagem dos handlers usa.

Não tem regra de negócio, I/O nem estado.

## Estilo arquitetural e padrões

O anel mais interno da Clean Architecture do ModularMonolith de referência (Practical.CleanArchitecture), abaixo até do Domain. Usa Dependency Inversion: expõe a abstração (`IDateTimeProvider`), e a implementação mora na Infrastructure.

## Dependências

| Pode referenciar | Não pode |
|---|---|
| só a BCL do .NET | qualquer outro projeto da solução |

Provado em `tests/Cashflow.ArchTests` (`CrossCuttingConcerns_nao_conhece_nenhum_outro_projeto`).

## Decisões

- [ADR-0009](../../docs/ultron/agents/adr/0009-monolito-modular.md): a estrutura modular.
- [`docs/ultron/agents/logging.md`](../../docs/ultron/agents/logging.md): por que comerciante e chave aparecem só como pseudônimo.
