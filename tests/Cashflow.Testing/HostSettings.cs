namespace Cashflow.Testing;

/// <summary>
/// A configuração entra por variável de ambiente porque o <c>Program</c> a lê antes do <c>Build</c>.
/// Cada fábrica escreve todas as chaves que usa ao montar o host: as fábricas sobem uma de cada
/// vez, e uma não herda o que a anterior deixou.
/// </summary>
internal static class HostSettings
{
    /// <summary>Escreve a autenticação dos testes, o nível de log e os <paramref name="valores"/> nas variáveis de ambiente.</summary>
    public static void Aplicar(IReadOnlyDictionary<string, string?> valores)
    {
        Environment.SetEnvironmentVariable("Auth__Issuer", TestTokens.Issuer);
        Environment.SetEnvironmentVariable("Auth__Audience", TestTokens.Audience);
        Environment.SetEnvironmentVariable("Auth__SigningKey", TestTokens.SigningKey);
        Environment.SetEnvironmentVariable("Serilog__MinimumLevel__Default", "Warning");

        foreach (var (chave, valor) in valores)
            Environment.SetEnvironmentVariable(chave, valor);
    }

    /// <summary>Liga ou desliga cada módulo, todos apontando para o banco do teste.</summary>
    public static Dictionary<string, string?> Modulos(string connectionString, bool lancamentos, bool consolidado) => new()
    {
        ["Modules__Lancamentos__Enabled"] = lancamentos.ToString(),
        ["Modules__Lancamentos__ConnectionStrings__Default"] = connectionString,
        ["Modules__Consolidado__Enabled"] = consolidado.ToString(),
        ["Modules__Consolidado__ConnectionStrings__Default"] = connectionString
    };
}
