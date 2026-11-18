using System.Security.Cryptography;
using System.Text;

namespace Cashflow.Common.Security;

public static class Hashes
{
    public static string Texto(string texto) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(texto))).ToLowerInvariant();

    public static string Comerciante(Guid id) =>
        Convert.ToHexString(SHA256.HashData(id.ToByteArray()))[..16].ToLowerInvariant();

    public static string Chave(string chave) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(chave)))[..16].ToLowerInvariant();
}
