using Cashflow.Blazor.Modules.Consolidado.Services;
using Cashflow.Blazor.Modules.Core.Services;
using Cashflow.Blazor.Modules.Infraestrutura;
using Cashflow.Blazor.Modules.Infraestrutura.Services;
using Cashflow.Blazor.Modules.Lancamentos.Services;
using Microsoft.Extensions.Options;

namespace Cashflow.Terminal.Tests;

/// <summary>Uma sessão com os clientes reais sobre uma API falsa: o teste exercita o caminho de produção.</summary>
internal static class SessaoDeTeste
{
    public static Sessao Para(string perfil, HttpMessageHandler? handler = null, InfraestruturaOptions? infraestrutura = null)
    {
        handler ??= new ApiFalsa(_ => ApiFalsa.Json("{}"));
        var token = new TokenFixo();
        return new Sessao
        {
            Perfil = perfil,
            Lancamentos = new LancamentoService(Http(handler), token),
            Consolidado = new ConsolidadoService(Http(handler), token),
            Saude = new SaudeService(Http(handler), Options.Create(infraestrutura ?? new InfraestruturaOptions())),
            Prometheus = new PrometheusService(Http(handler)),
            Logs = new LogsService(Http(handler)),
            Relogio = TimeProvider.System
        };
    }

    private static HttpClient Http(HttpMessageHandler handler) =>
        new(handler) { BaseAddress = new Uri("http://api/") };

    private sealed class TokenFixo : ITokenManager
    {
        public Task<TokenModel> GetTokenAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new TokenModel("token", DateTimeOffset.UtcNow.AddHours(1)));
    }
}
