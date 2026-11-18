namespace Cashflow.CrossCuttingConcerns.Exceptions;

public class NotFoundException(string mensagem) : Exception(mensagem);
