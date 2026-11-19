using Cashflow.Blazor.Modules.Core.Authorization;
using Cashflow.Blazor.Modules.Core.Services;
using Cashflow.BlazorServerSide.Components;
using Cashflow.BlazorServerSide.ConfigurationOptions;
using Cashflow.BlazorServerSide.Services;
using Cashflow.Infrastructure.Logging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

// O frontend é mais um cliente da API (ADR-0010): só fala HTTP com lancamentos-api e
// consolidado-api, e o token de acesso nunca sai do servidor.
var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

var appSettings = new AppSettings();
configuration.Bind(appSettings);

if (string.IsNullOrWhiteSpace(appSettings.Auth.SigningKey) || appSettings.Auth.SigningKey.Length < 32)
    throw new InvalidOperationException("Auth:SigningKey ausente ou com menos de 32 caracteres");

builder.Host.UseCashflowLogging(appSettings.ServiceName);

var services = builder.Services;
services.AddSingleton(appSettings);

services.AddRazorComponents()
    .AddInteractiveServerComponents();

services.AddCashflowBlazorModules(
    new Uri(appSettings.ResourceServer.Lancamentos),
    new Uri(appSettings.ResourceServer.Consolidado));

services.AddCashflowInfraestrutura(configuration);

services.AddSingleton<UsuarioStore>();
services.AddSingleton<AccessTokenIssuer>();
services.AddScoped<ITokenManager, TokenManager>();

services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "cashflow.session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/acesso-negado";
    });
services.AddAuthorizationBuilder().AddCashflowPolicies();
services.AddCascadingAuthenticationState();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// Só GET: reexecutar um POST recusado (ex.: antiforgery inválido no /logout) releria o formulário.
app.UseWhen(context => HttpMethods.IsGet(context.Request.Method), branch =>
    branch.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
// RF-8: o processo vivo e o serviço pronto respondem separados, como nos demais hosts.
app.MapGet("/health/live", () => Results.Ok(new { status = "live" })).AllowAnonymous();
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready" })).AllowAnonymous();

// Formulário com antiforgery: ninguém desloga o usuário por um link num site de terceiros.
// O parâmetro [FromForm] é o que liga a validação automática de antiforgery do minimal API.
app.MapPost("/logout", async (HttpContext context, [FromForm] string? returnUrl) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.LocalRedirect("/login");
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Cashflow.Blazor.Modules.Core.Pages.Index).Assembly);

app.Run();
