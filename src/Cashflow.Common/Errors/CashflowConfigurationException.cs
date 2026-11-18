namespace Cashflow.Common.Errors;

public sealed class CashflowConfigurationException(string mensagem) : InvalidOperationException(mensagem);
