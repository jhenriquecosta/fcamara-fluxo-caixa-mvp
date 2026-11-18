namespace Cashflow.Infrastructure.Web.Problems;

public sealed class CashflowConfigurationException(string mensagem) : InvalidOperationException(mensagem);
