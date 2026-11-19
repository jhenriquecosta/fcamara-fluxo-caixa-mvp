using Cashflow.Testing;

namespace Cashflow.EndToEndTests;

/// <summary>
/// A topologia do compose dentro do processo de teste: o WebAPI duas vezes (lancamentos-api e
/// consolidado-api) e o BackgroundServer com o outbox e o consumidor.
/// </summary>
internal sealed class Topologia : IDisposable
{
    /// <summary>O relógio da lancamentos-api: 02h30 de 18/11 em UTC, que é 23h30 de 17/11 em São Paulo.</summary>
    public static readonly DateTimeOffset Agora = new(2026, 11, 18, 2, 30, 0, TimeSpan.Zero);

    /// <summary>O dia de negócio em que os lançamentos feitos às <see cref="Agora"/> caem.</summary>
    public const string Dia = "2026-11-17";

    private readonly WebApiFactory _lancamentos;
    private readonly WebApiFactory _consolidado;
    private readonly BackgroundServerFactory _trabalhoDeFundo;

    public Topologia(PostgresFixture db, KafkaFixture kafka)
    {
        _lancamentos = WebApiFactory.SoLancamentos(db.ConnectionString, Agora);
        _consolidado = WebApiFactory.SoConsolidado(db.ConnectionString);
        _trabalhoDeFundo = new BackgroundServerFactory(db.ConnectionString, kafka.BootstrapServers).Iniciar();
    }

    /// <summary>Um cliente da lancamentos-api, com escopo de escrita e uma chave de idempotência nova.</summary>
    public HttpClient Lancamentos(Guid comerciante) =>
        _lancamentos.CreateClient().Autenticado(comerciante, "lancamentos.write", Guid.NewGuid().ToString());

    /// <summary>Um cliente da consolidado-api, com escopo de leitura.</summary>
    public HttpClient Consolidado(Guid comerciante) =>
        _consolidado.CreateClient().Autenticado(comerciante, "consolidado.read");

    public void Dispose()
    {
        _trabalhoDeFundo.Dispose();
        _consolidado.Dispose();
        _lancamentos.Dispose();
    }
}
