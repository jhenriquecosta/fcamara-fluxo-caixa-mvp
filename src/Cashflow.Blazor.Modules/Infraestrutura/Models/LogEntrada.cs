namespace Cashflow.Blazor.Modules.Infraestrutura.Models;

/// <summary>Uma linha de log como o Serilog gravou no Elasticsearch.</summary>
/// <param name="Instante">Quando aconteceu.</param>
/// <param name="Nivel">Information, Warning, Error...</param>
/// <param name="Servico">O serviço que escreveu.</param>
/// <param name="Mensagem">A mensagem renderizada.</param>
/// <param name="Origem">O <c>SourceContext</c>, quando há.</param>
public sealed record LogEntrada(DateTimeOffset Instante, string Nivel, string Servico, string Mensagem, string? Origem);
