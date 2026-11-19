namespace Cashflow.CrossCuttingConcerns.Exceptions;

/// <summary>
/// A configuração obrigatória está ausente ou inválida (connection string, tópico, broker).
/// É lançada na subida do host, para que um deploy mal configurado falhe na hora e não no primeiro pedido.
/// </summary>
public sealed class CashflowConfigurationException(string mensagem) : InvalidOperationException(mensagem);
