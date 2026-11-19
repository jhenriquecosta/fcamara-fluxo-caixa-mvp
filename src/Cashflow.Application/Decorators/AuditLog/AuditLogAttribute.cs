namespace Cashflow.Application.Decorators.AuditLog;

/// <summary>
/// Pede o decorator de auditoria: o handler marcado tem cada execução registrada no log, com o
/// nome do caso de uso e a duração. Os dados pessoais não entram, só o tipo.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class AuditLogAttribute : Attribute;
