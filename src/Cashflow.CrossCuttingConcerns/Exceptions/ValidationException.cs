namespace Cashflow.CrossCuttingConcerns.Exceptions;

public class ValidationException(string mensagem) : Exception(mensagem);
