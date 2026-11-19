namespace Cashflow.Application;

/// <summary>
/// Um caso de uso que muda estado (CQRS, lado da escrita). Quem o aciona monta o comando e o
/// entrega ao <see cref="Dispatcher"/>; o resultado, quando existe, volta numa propriedade do próprio comando.
/// </summary>
public interface ICommand;
