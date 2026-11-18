using System.Globalization;
using Cashflow.Common.Security;
using Cashflow.Lancamentos.Domain;

namespace Cashflow.Lancamentos.Application;

public static class LancamentoCorpo
{
    public static string Hash(TipoLancamento tipo, decimal valor, string? descricao)
    {
        var texto = $"{valor.ToString("0.00", CultureInfo.InvariantCulture)}|{tipo.Texto()}|{descricao ?? ""}";
        return Hashes.Texto(texto);
    }
}
