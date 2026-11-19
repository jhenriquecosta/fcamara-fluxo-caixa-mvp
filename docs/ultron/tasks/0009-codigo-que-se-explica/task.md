# Task 0009-codigo-que-se-explica — Código que se explica: nomes, documentação, testes e README por projeto

tipo: refactor
branch: refactor/0009-codigo-que-se-explica
estado: em andamento
responsavel: jhenriquecosta@hotmail.com
next_skill: ultron-coder
issue: #25

O código é a documentação. Hoje há classes e métodos que não dizem o que são nem por que existem, e testes que não contam a regra que provam. Escopo: (1) apagar pastas órfãs (resíduos de bin/obj das estruturas anteriores); (2) um README.md por projeto: por que existe, finalidade, padrões e estilo arquitetural a que adere, o que pode e não pode referenciar; (3) nomes que revelam intenção e XML docs que explicam o porquê em tipos e métodos públicos; (4) testes que contam a regra de negócio (nome + Arrange/Act/Assert legível). Restrição: a sessão do front (feature/0008-frontend-blazor) usa Scopes, CashflowAuth.ComercianteId, CashflowProblems, Dispatcher, DiaDeNegocio.De, IDateTimeProvider.OffsetUtcNow, ILancamentoRepository, DbContext.GetConnectionAsync, AuditLog, DatabaseRetry, IQuery/IQueryHandler: esses nomes não mudam aqui. Comportamento observável inalterado: 57/57 verdes.
