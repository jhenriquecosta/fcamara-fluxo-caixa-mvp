namespace Cashflow.Infrastructure.Web.OpenApi;

/// <summary>
/// Marca o endpoint cujo POST exige o cabeçalho <c>Idempotency-Key</c>. O header é lido de
/// <c>request.Headers</c>, não é parâmetro de assinatura — por isso a metadata, que o operation
/// transformer traduz em parâmetro e resposta 400 no documento (ADR-0013).
/// </summary>
public sealed class IdempotencyKeyMetadata;
