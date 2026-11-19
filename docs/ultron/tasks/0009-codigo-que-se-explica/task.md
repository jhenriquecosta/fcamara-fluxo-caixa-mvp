# Task 0009-codigo-que-se-explica — Código que se explica: nomes, documentação, testes e README por projeto

tipo: refactor
branch: refactor/0009-codigo-que-se-explica
estado: em andamento
responsavel: jhenriquecosta@hotmail.com
next_skill: ultron-coder
issue: #25

O código é a documentação. Hoje há classes e métodos que não dizem o que são nem por que existem, e testes que não contam a regra que provam. Escopo: (1) apagar pastas órfãs (resíduos de bin/obj das estruturas anteriores); (2) um README.md por projeto: por que existe, finalidade, padrões e estilo arquitetural a que adere, o que pode e não pode referenciar; (3) nomes que revelam intenção e XML docs que explicam o porquê em tipos e métodos públicos; (4) testes que contam a regra de negócio (nome + Arrange/Act/Assert legível). Restrição: a sessão do front (feature/0008-frontend-blazor) usa Scopes, CashflowAuth.ComercianteId, CashflowProblems, Dispatcher, DiaDeNegocio.De, IDateTimeProvider.OffsetUtcNow, ILancamentoRepository, DbContext.GetConnectionAsync, AuditLog, DatabaseRetry, IQuery/IQueryHandler: esses nomes não mudam aqui. Comportamento observável inalterado: 57/57 verdes.

## Plano

1. **Órfãos:** apagar as pastas das estruturas anteriores que só guardam `bin/`/`obj/`; apagar o código que ninguém usa (tipos copiados da referência sem uso: `Specification`, `Money`, `NotFoundException`, `ValidationException`, `IConcurrencyHandler`, `IHasKey`, membros mortos do `IDateTimeProvider` e do `IMessageBus`). Abstração sem uso mente sobre o sistema.
2. **Nomes que revelam intenção**, onde o nome atual esconde o que faz (`Hashes` → pseudônimo de log, `RegistroResultado` → resultado do lançamento, `Reivindicacao` → reserva da chave, `Utils`, `Mapear`, `Delta`, `GetAsync`...). Convenção: o framework de aplicação herdado da referência fica em inglês (`ICommand`, `HandleAsync`, `UnitOfWork`); o domínio fala português (linguagem ubíqua).
3. **XML docs** em todo tipo e membro público: o que é, por que existe, que regra protege.
4. **Testes que contam a regra:** nome é a regra em frase, corpo em Arrange/Act/Assert, critério de aceite em `[Trait("Criterio", "ACn")]` em vez de prefixo no nome.
5. **README.md por projeto:** por que existe, finalidade, padrões e estilo arquitetural, dependências permitidas e proibidas, o que tem dentro.

Nomes congelados (a sessão do front usa): `Scopes`, `CashflowAuth.ComercianteId`, `CashflowProblems.*`, `Dispatcher`, `DiaDeNegocio.De`, `IDateTimeProvider.OffsetUtcNow`, `ILancamentoRepository`, `DbContext.GetConnectionAsync`, `[AuditLog]`, `[DatabaseRetry]`, `IQuery`/`IQueryHandler`, `TestTokens`, `WebApiFactory.SoLancamentos`.
