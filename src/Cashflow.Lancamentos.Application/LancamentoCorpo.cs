using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Cashflow.Lancamentos.Domain;

namespace Cashflow.Lancamentos.Application;

public static class LancamentoCorpo
{
    public static string Hash(TipoLancamento tipo, decimal valor, string? descricao)
    {
        var texto = $"{valor.ToString("0.00", CultureInfo.InvariantCulture)}|{tipo.Texto()}|{descricao ?? ""}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(texto))).ToLowerInvariant();
    }

    public static string HashComerciante(Guid id) =>
        Convert.ToHexString(SHA256.HashData(id.ToByteArray()))[..16].ToLowerInvariant();

    public static string HashChave(string chave) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(chave)))[..16].ToLowerInvariant();
}
