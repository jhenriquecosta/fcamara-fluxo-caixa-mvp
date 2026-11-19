using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cashflow.Blazor.Modules.Core.Services;

namespace Cashflow.Terminal;

/// <summary>
/// O <c>ITokenManager</c> do terminal: pede o JWT ao <c>/dev/token</c> do ambiente de
/// desenvolvimento (o mesmo que o demo do front usa) e o mantém até perto do vencimento.
/// </summary>
public sealed class DevTokenManager(HttpClient httpClient, Guid comercianteId, string[] escopos) : ITokenManager
{
    private TokenModel? _atual;

    /// <inheritdoc />
    public async Task<TokenModel> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        var agora = DateTimeOffset.UtcNow;
        if (_atual is { } atual && !atual.ExpiraAntesDe(agora))
            return atual;

        using var resposta = await httpClient.PostAsJsonAsync(
            "dev/token", new { comercianteId, scopes = escopos }, cancellationToken);
        resposta.EnsureSuccessStatusCode();
        var json = await resposta.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var jwt = json.GetProperty("token").GetString()
            ?? throw new ApiException(HttpStatusCode.BadGateway, "resposta inválida do serviço");
        return _atual = new TokenModel(jwt, Vencimento(jwt));
    }

    private static DateTimeOffset Vencimento(string jwt)
    {
        try
        {
            var parte = jwt.Split('.')[1].Replace('-', '+').Replace('_', '/');
            parte = parte.PadRight(parte.Length + (4 - parte.Length % 4) % 4, '=');
            var json = JsonSerializer.Deserialize<JsonElement>(Convert.FromBase64String(parte));
            return json.TryGetProperty("exp", out var exp)
                ? DateTimeOffset.FromUnixTimeSeconds(exp.GetInt64())
                : DateTimeOffset.UtcNow.AddMinutes(5);
        }
        catch (Exception ex) when (ex is JsonException or FormatException or IndexOutOfRangeException)
        {
            return DateTimeOffset.UtcNow.AddMinutes(5);
        }
    }
}

/// <summary>Os escopos do contrato da API, espelhados aqui porque o terminal é só mais um cliente HTTP.</summary>
public static class EscoposDoContrato
{
    /// <summary>Registrar e estornar lançamentos.</summary>
    public const string LancamentosWrite = "lancamentos.write";

    /// <summary>Listar lançamentos.</summary>
    public const string LancamentosRead = "lancamentos.read";

    /// <summary>Consultar o consolidado.</summary>
    public const string ConsolidadoRead = "consolidado.read";

    /// <summary>Os escopos de cada perfil, como o front emite (ADR-0010).</summary>
    /// <param name="perfil">O perfil escolhido.</param>
    public static string[] DoPerfil(string perfil) => perfil == "operador"
        ? [LancamentosWrite, LancamentosRead, ConsolidadoRead]
        : [LancamentosRead, ConsolidadoRead];
}
