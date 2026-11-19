namespace Cashflow.Blazor.Modules.Infraestrutura.Models;

/// <summary>O resultado de uma checagem de saúde.</summary>
/// <param name="Nome">O serviço.</param>
/// <param name="Papel">O papel do serviço na solução.</param>
/// <param name="Disponivel">Respondeu 2xx dentro do prazo.</param>
/// <param name="StatusCode">O status HTTP, se houve resposta.</param>
/// <param name="LatenciaMs">O tempo até a resposta.</param>
/// <param name="Erro">Por que está fora, quando está.</param>
public sealed record SaudeServico(
    string Nome, string Papel, bool Disponivel, int? StatusCode, double LatenciaMs, string? Erro);
