using System.Net.Sockets;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Cashflow.Infrastructure.Web;

/// <summary>
/// As dependências que o readiness confere: o banco de cada módulo ligado no deploy (RF-8).
/// Liveness responde enquanto o processo vive; readiness responde pelo que ele precisa para servir.
/// </summary>
public static class SaudeDaApi
{
    private static readonly TimeSpan Prazo = TimeSpan.FromSeconds(2);

    /// <summary>Os módulos cujo banco não respondeu; vazio quando todos respondem.</summary>
    /// <param name="configuration">A configuração que diz quais módulos estão ligados e seus bancos.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    public static async Task<IReadOnlyList<string>> DependenciasForaAsync(
        IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var problemas = new List<string>();
        await VerificarAsync(configuration, "Lancamentos", problemas, cancellationToken);
        await VerificarAsync(configuration, "Consolidado", problemas, cancellationToken);
        return problemas;
    }

    private static async Task VerificarAsync(
        IConfiguration configuration, string modulo, List<string> problemas, CancellationToken cancellationToken)
    {
        if (!bool.TryParse(configuration[$"Modules:{modulo}:Enabled"], out var ligado) || !ligado)
            return;

        var connectionString = configuration[$"Modules:{modulo}:ConnectionStrings:Default"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            problemas.Add($"{modulo}: sem connection string");
            return;
        }

        using var prazo = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        prazo.CancelAfter(Prazo);
        try
        {
            await using var conexao = new NpgsqlConnection(connectionString);
            await conexao.OpenAsync(prazo.Token);
            await using var comando = new NpgsqlCommand("SELECT 1", conexao);
            await comando.ExecuteScalarAsync(prazo.Token);
        }
        catch (Exception ex) when (ex is NpgsqlException or SocketException or TimeoutException or OperationCanceledException)
        {
            // O /health/ready é anônimo: a mensagem do driver pode trazer host, porta e usuário do banco.
            problemas.Add($"{modulo}: banco indisponível");
        }
    }
}
