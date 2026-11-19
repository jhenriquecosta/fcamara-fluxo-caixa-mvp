using System.Security.Cryptography;
using System.Text;

namespace Cashflow.CrossCuttingConcerns.Security;

/// <summary>
/// Pseudônimos estáveis para dados que não podem aparecer em claro nos logs (LGPD e
/// <c>docs/ultron/agents/logging.md</c>). O mesmo valor sempre gera o mesmo pseudônimo, então
/// dá para seguir um comerciante ou uma chave de ponta a ponta sem expor quem ele é.
/// </summary>
public static class PseudonimoDeLog
{
    /// <summary>Os 16 primeiros caracteres do SHA-256 do id do comerciante: o que os logs mostram no lugar dele.</summary>
    public static string DoComerciante(Guid comercianteId) =>
        Sha256Hex(comercianteId.ToByteArray())[..16];

    /// <summary>Os 16 primeiros caracteres do SHA-256 da chave de idempotência enviada pelo cliente.</summary>
    public static string DaChaveDeIdempotencia(string chave) =>
        Sha256Hex(Encoding.UTF8.GetBytes(chave))[..16];

    /// <summary>
    /// O SHA-256 completo, em hexadecimal minúsculo. Serve de impressão digital de um texto,
    /// como o corpo que uma chave de idempotência protege.
    /// </summary>
    public static string ImpressaoDigital(string texto) =>
        Sha256Hex(Encoding.UTF8.GetBytes(texto));

    private static string Sha256Hex(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}
