using System.Globalization;
using Cashflow.CrossCuttingConcerns.Security;
using Cashflow.Modules.Lancamentos.Entities;

namespace Cashflow.Modules.Lancamentos.Commands;

public static class LancamentoCorpo
{
    public static string Hash(TipoLancamento tipo, decimal valor, string? descricao)
    {
        var texto = $"{valor.ToString("0.00", CultureInfo.InvariantCulture)}|{tipo.Texto()}|{descricao ?? ""}";
        return Hashes.Texto(texto);
    }
}
