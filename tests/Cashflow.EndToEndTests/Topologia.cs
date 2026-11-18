using Cashflow.Testing;

namespace Cashflow.EndToEndTests;

/// <summary>
/// A topologia do compose dentro do processo de teste: o WebAPI duas vezes (lancamentos-api e
/// consolidado-api) e o BackgroundServer com o outbox e o consumidor.
/// </summary>
internal sealed class Topologia : IDisposable
{
    public static readonly DateTimeOffset Agora = new(2026, 11, 18, 2, 30, 0, TimeSpan.Zero); // São Paulo: 2026-11-17 23:30
    public const string Dia = "2026-11-17";

    private readonly WebApiFactory _lancamentos;
    private readonly WebApiFactory _consolidado;
    private readonly BackgroundServerFactory _fundo;

    public Topologia(PostgresFixture db, KafkaFixture kafka)
    {
        _lancamentos = WebApiFactory.SoLancamentos(db.ConnectionString, Agora);
        _consolidado = WebApiFactory.SoConsolidado(db.ConnectionString);
        _fundo = new BackgroundServerFactory(db.ConnectionString, kafka.BootstrapServers).Iniciar();
    }

    public HttpClient Lancamentos(Guid comerciante) =>
        _lancamentos.CreateClient().Autenticado(comerciante, "lancamentos.write", Guid.NewGuid().ToString());

    public HttpClient Consolidado(Guid comerciante) =>
        _consolidado.CreateClient().Autenticado(comerciante, "consolidado.read");

    public void Dispose()
    {
        _fundo.Dispose();
        _consolidado.Dispose();
        _lancamentos.Dispose();
    }
}
