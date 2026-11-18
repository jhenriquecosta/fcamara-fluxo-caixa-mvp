using System.Globalization;
using System.Security.Claims;
using System.Text;
using Cashflow.Consolidado.Api.Application;
using Cashflow.Consolidado.Api.Auth;
using Cashflow.Consolidado.Api.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
        .Enrich.WithProperty("service", "consolidado")
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
builder.Services.AddSingleton(_ => new NpgsqlDataSourceBuilder(
        builder.Configuration.GetConnectionString("Postgres")
        ?? throw new InvalidOperationException("ConnectionStrings:Postgres não configurada")).Build());
builder.Services.AddSingleton<ConsolidadoService>();
builder.Services.AddHostedService<ConsolidadoConsumer>();

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

app.MapGet("/api/v1/consolidado/{data}", async (
    string data,
    ClaimsPrincipal user,
    ConsolidadoService service,
    CancellationToken ct) =>
{
    if (!DateOnly.TryParseExact(data, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var dia))
        return Results.Problem(statusCode: 400, title: "data inválida; use yyyy-MM-dd");

    if (!Guid.TryParse(user.FindFirst("comercianteId")?.Value, out var comercianteId))
        return Results.Problem(statusCode: 403, title: "token sem comercianteId");

    var resposta = await service.ObterAsync(comercianteId, dia, ct);
    return Results.Ok(resposta);
}).RequireAuthorization("consolidado.read");

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
