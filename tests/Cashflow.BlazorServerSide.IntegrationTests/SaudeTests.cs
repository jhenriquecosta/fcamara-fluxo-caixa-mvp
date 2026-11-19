using System.Net;

namespace Cashflow.BlazorServerSide.IntegrationTests;

/// <summary>RF-8 no host do frontend: liveness e readiness para o balanceador.</summary>
public sealed class SaudeTests
{
    [Fact]
    [Trait("Criterio", "RF8")]
    public async Task Live_e_ready_respondem_200_no_front()
    {
        await using var front = new FrontendFactory(() => new HttpClientHandler());
        using var cliente = front.CreateClient();

        Assert.Equal(HttpStatusCode.OK,
            (await cliente.GetAsync("/health/live", TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await cliente.GetAsync("/health/ready", TestContext.Current.CancellationToken)).StatusCode);
    }
}
