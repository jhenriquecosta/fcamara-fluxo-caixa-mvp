# Task 0009-codigo-que-se-explica — Código que se explica: nomes, documentação, testes e README por projeto

tipo: refactor
branch: refactor/0009-codigo-que-se-explica
publicacao: pendente
estado: concluído
responsavel: jhenriquecosta@hotmail.com
next_skill: 
issue: #25

O código é a documentação. Hoje há classes e métodos que não dizem o que são nem por que existem, e testes que não contam a regra que provam. Escopo: (1) apagar pastas órfãs (resíduos de bin/obj das estruturas anteriores); (2) um README.md por projeto: por que existe, finalidade, padrões e estilo arquitetural a que adere, o que pode e não pode referenciar; (3) nomes que revelam intenção e XML docs que explicam o porquê em tipos e métodos públicos; (4) testes que contam a regra de negócio (nome + Arrange/Act/Assert legível). Restrição: a sessão do front (feature/0008-frontend-blazor) usa Scopes, CashflowAuth.ComercianteId, CashflowProblems, Dispatcher, DiaDeNegocio.De, IDateTimeProvider.OffsetUtcNow, ILancamentoRepository, DbContext.GetConnectionAsync, AuditLog, DatabaseRetry, IQuery/IQueryHandler: esses nomes não mudam aqui. Comportamento observável inalterado: 57/57 verdes.

## Plano

1. **Órfãos:** apagar as pastas das estruturas anteriores que só guardam `bin/`/`obj/`; apagar o código que ninguém usa (tipos copiados da referência sem uso: `Specification`, `Money`, `NotFoundException`, `ValidationException`, `IConcurrencyHandler`, `IHasKey`, membros mortos do `IDateTimeProvider` e do `IMessageBus`). Abstração sem uso mente sobre o sistema.
2. **Nomes que revelam intenção**, onde o nome atual esconde o que faz (`Hashes` → pseudônimo de log, `RegistroResultado` → resultado do lançamento, `Reivindicacao` → reserva da chave, `Utils`, `Mapear`, `Delta`, `GetAsync`...). Convenção: o framework de aplicação herdado da referência fica em inglês (`ICommand`, `HandleAsync`, `UnitOfWork`); o domínio fala português (linguagem ubíqua).
3. **XML docs** em todo tipo e membro público: o que é, por que existe, que regra protege.
4. **Testes que contam a regra:** nome é a regra em frase, corpo em Arrange/Act/Assert, critério de aceite em `[Trait("Criterio", "ACn")]` em vez de prefixo no nome.
5. **README.md por projeto:** por que existe, finalidade, padrões e estilo arquitetural, dependências permitidas e proibidas, o que tem dentro.

Nomes congelados (a sessão do front usa): `Scopes`, `CashflowAuth.ComercianteId`, `CashflowProblems.*`, `Dispatcher`, `DiaDeNegocio.De`, `IDateTimeProvider.OffsetUtcNow`, `ILancamentoRepository`, `DbContext.GetConnectionAsync`, `[AuditLog]`, `[DatabaseRetry]`, `IQuery`/`IQueryHandler`, `TestTokens`, `WebApiFactory.SoLancamentos`.

## Critérios de aceitação

- [x] Pastas órfãs apagadas: 10 diretórios das estruturas anteriores (só `bin/`/`obj/`). Fora de `src/` e `tests/`, todo arquivo versionado é referenciado; `.tmp/` é material pessoal do Operador, fora do git, e ficou.
- [x] Código morto removido: `Specification`, `Money`, `NotFoundException`, `ValidationException`, `IConcurrencyHandler`, `IHasKey`, `UpdatedDateTime`, `IUnitOfWork.SaveChangesAsync`, `IDateTimeProvider.Now/UtcNow/OffsetNow`, `IMessageBus.ReceiveAsync(action)`.
- [x] Nomes que revelam intenção nos pontos opacos, preservando os nomes congelados da sessão do front.
- [x] Todo tipo e membro público de `src/` documentado, com CS1591 como erro em `src/Directory.Build.props`.
- [x] Testes: nome é a regra, Arrange/Act/Assert, critério em `[Trait("Criterio", "ACn")]`; testes com várias regras separados. Suíte 57 → 77, verde.
- [x] README.md em cada um dos 17 projetos: por que existe, finalidade, padrões e estilo, dependências permitidas e proibidas.
- [x] Comportamento observável inalterado (os mesmos ACs verdes; o novo teste de estorno de outro comerciante confirma o 404 que já existia).

## Exceções registradas

- **verify do fechamento pulado (`--no-verify`):** ultron-skills#481 (o `-q` do `verify_all.sh` no .NET 10/MTP). Verificado pelos comandos do CI: build sem aviso, `dotnet format --verify-no-changes`, suíte 77/77; o CI do PR verifica de novo.
