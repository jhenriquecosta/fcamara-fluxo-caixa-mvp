using System.Net.Http.Headers;

namespace Cashflow.Terminal.Tests;

/// <summary>Uma API falsa que registra o que o terminal enviou e responde com um roteiro.</summary>
internal sealed class ApiFalsa(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
{
    public List<Requisicao> Recebidas { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var corpo = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Recebidas.Add(new Requisicao(request.Method, request.RequestUri!, corpo, ChaveDeIdempotencia(request.Headers)));
        return responder(request);
    }

    private static string? ChaveDeIdempotencia(HttpRequestHeaders headers) =>
        headers.TryGetValues("Idempotency-Key", out var valores) ? valores.SingleOrDefault() : null;

    public static HttpResponseMessage Json(string json, int status = 200) => new((System.Net.HttpStatusCode)status)
    {
        Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
    };

    internal sealed record Requisicao(HttpMethod Metodo, Uri Url, string? Corpo, string? ChaveDeIdempotencia);
}
