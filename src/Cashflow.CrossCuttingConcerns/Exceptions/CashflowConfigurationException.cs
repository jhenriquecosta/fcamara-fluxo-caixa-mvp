namespace Cashflow.CrossCuttingConcerns.Exceptions;

public sealed class CashflowConfigurationException(string mensagem) : InvalidOperationException(mensagem);
