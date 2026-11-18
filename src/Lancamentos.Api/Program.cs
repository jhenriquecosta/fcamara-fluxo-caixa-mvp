using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Cashflow.Lancamentos.Api.Application;
using Cashflow.Lancamentos.Api.Auth;
using Cashflow.Lancamentos.Api.Infrastructure;
using Cashflow.Lancamentos.Api.Observability;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("service", "lancamentos")
        .Enrich.WithProperty("env", context.HostingEnvironment.EnvironmentName)
        .WriteTo.Console(new CompactJsonFormatter());

    var elasticsearchUrl = context.Configuration["Serilog:ElasticsearchUrl"];
    if (!string.IsNullOrWhiteSpace(elasticsearchUrl))
    {
        configuration.WriteTo.Elasticsearch(new Serilog.Sinks.Elasticsearch.ElasticsearchSinkOptions(new Uri(elasticsearchUrl))
        {
            AutoRegisterTemplate = false,
            IndexFormat = "cashflow-logs-{0:yyyy.MM.dd}"
        });
    }
});

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<LancamentosMetrics>();
builder.Services.AddSingleton(_ => new NpgsqlDataSourceBuilder(
        builder.Configuration.GetConnectionString("Postgres")
        ?? throw new InvalidOperationException("ConnectionStrings:Postgres não configurada")).Build());
builder.Services.AddSingleton<LancamentoService>();
builder.Services.AddHostedService<OutboxPublisher>();

var signingKey = new SymmetricSecurityKey(
    Encoding.UTF8.GetBytes(builder.Configuration["Auth:SigningKey"]
        ?? throw new InvalidOperationException("Auth:SigningKey não configurada")));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = builder.Configuration["Auth:Issuer"],
            ValidAudience = builder.Configuration["Auth:Audience"],
            IssuerSigningKey = signingKey,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Log.Warning("autenticação falhou: {Reason}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnForbidden = context =>
            {
                Log.Warning("autorização negada para {Path}", context.Request.Path);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("lancamentos.write", policy =>
        policy.RequireAssertion(context => HasScope(context.User, "lancamentos.write")));
    options.AddPolicy("consolidado.read", policy =>
        policy.RequireAssertion(context => HasScope(context.User, "consolidado.read")));
});

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddMeter(LancamentosMetrics.MeterName)
        .AddPrometheusExporter());

var app = builder.Build();

await Migrator.ApplyAsync(app.Services.GetRequiredService<NpgsqlDataSource>());

app.UseSerilogRequestLogging();

app.Use(async (context, next) =>
{
    var requestId = context.Request.Headers["X-Request-Id"].FirstOrDefault()
                    ?? Guid.NewGuid().ToString("N");
    var traceId = System.Diagnostics.Activity.Current?.TraceId.ToString();
    using (Serilog.Context.LogContext.PushProperty("request_id", requestId))
    using (Serilog.Context.LogContext.PushProperty("correlation_id", traceId ?? requestId))
    using (Serilog.Context.LogContext.PushProperty("trace_id", traceId ?? ""))
    {
        context.Response.Headers["X-Request-Id"] = requestId;
        await next();
    }
});

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

app.MapPost("/api/v1/lancamentos", async (
    HttpRequest request,
    ClaimsPrincipal user,
    LancamentoService service,
    CancellationToken ct) =>
{
    var chave = request.Headers["Idempotency-Key"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(chave))
        return Results.Problem(statusCode: 400, title: "Idempotency-Key é obrigatório");

    LancamentoRequest? body;
    try
    {
        body = await request.ReadFromJsonAsync<LancamentoRequest>(ct);
    }
    catch (JsonException)
    {
        return Results.Problem(statusCode: 400, title: "corpo inválido");
    }

    if (body is null || body.Valor <= 0 ||
        !Enum.TryParse<TipoLancamento>(body.Tipo ?? string.Empty, ignoreCase: true, out var tipo))
    {
        return Results.Problem(statusCode: 422, title: "lançamento inválido",
            detail: "valor deve ser maior que zero e tipo deve ser debito ou credito");
    }

    if (!Guid.TryParse(user.FindFirst("comercianteId")?.Value, out var comercianteId))
        return Results.Problem(statusCode: 403, title: "token sem comercianteId");

    var resultado = await service.RegistrarAsync(
        new RegistrarLancamento(comercianteId, tipo, body.Valor, body.Descricao, chave), ct);

    return resultado switch
    {
        RegistroResultado.Criado criado => Results.Json(criado.Resposta, statusCode: 201),
        RegistroResultado.Idempotente idempotente =>
            Results.Content(idempotente.RespostaJson, "application/json", statusCode: 201),
        RegistroResultado.PayloadDivergente =>
            Results.Problem(statusCode: 422, title: "mesma idempotency key com corpo diferente"),
        RegistroResultado.EmProcessamento =>
            Results.Problem(statusCode: 409, title: "requisição em processamento para esta idempotency key"),
        _ => Results.Problem(statusCode: 500)
    };
}).RequireAuthorization("lancamentos.write");

if (app.Environment.IsDevelopment())
{
    app.MapPost("/dev/token", (DevTokenRequest request, IConfiguration configuration) =>
            Results.Ok(new
            {
                token = DevToken.Create(configuration, request.ComercianteId,
                    request.Scopes ?? new[] { "lancamentos.write", "consolidado.read" })
            }))
        .AllowAnonymous();
}

app.MapPrometheusScrapingEndpoint();

app.Run();

static bool HasScope(ClaimsPrincipal user, string scope) =>
    user.FindFirst("scope")?.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(scope) == true;

public partial class Program { }
