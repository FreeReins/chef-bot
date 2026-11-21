using ChefBot.Api;
using ChefBot.Core;
using ChefBot.Core.Managers.Contracts;
using JasperFx;
using Wolverine;
using Wolverine.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddChefBot(builder.Configuration);
builder.Services.AddChefBotSignIn(builder.Configuration);
builder.Logging.AddChefBotLogViewer();

// HTTP always; HTTPS too when Https:Mode is SelfSigned or Certificate.
var certificates = builder.Services.AddChefBotHttps(builder.Configuration);
builder.WebHost.ConfigureKestrel(certificates.Configure);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ChefBotExceptionHandler>();
builder.Services.AddOpenApi();
// Request and response bodies use JSON made at build time (each enum says it is written as text).
builder.Services.ConfigureHttpJsonOptions(json =>
{
    json.SerializerOptions.TypeInfoResolverChain.Insert(0, ContractJsonContext.Default);
    json.SerializerOptions.TypeInfoResolverChain.Insert(1, ApiJsonContext.Default);
});

// Wolverine runs the HTTP endpoints (Endpoints/) and the message handlers (Messaging/). Messages the managers
// publish are kept in PostgreSQL with their data, so they survive a restart.
builder.Host.UseWolverine(wolverine =>
{
    wolverine.UseChefBotMessageStorage(builder.Configuration);
    wolverine.ResolveChefBotServicesFromContainer();
});
builder.Services.UseChefBotGeneratedCode();
builder.Services.AddWolverineHttp();

var app = builder.Build();

app.UseExceptionHandler();
app.UseChefBotSecurityHeaders();
app.UseChefBotProxyHeaders();
app.UseChefBotHttpsRedirect();
app.UseDefaultFiles();
// The web page is built from src/ChefBot.Web. Vite names the files under /assets after their content, so they
// can be cached for good; index.html is checked on every visit, so an upgrade shows up straight away.
var webPage = new StaticFileOptions
{
    OnPrepareResponse = file => file.Context.Response.Headers.CacheControl =
        file.Context.Request.Path.StartsWithSegments("/assets") ? "public, max-age=31536000, immutable" : "no-cache",
};
app.UseStaticFiles(webPage);
app.UseAuthentication();
app.UseAuthorization();
app.MapOpenApi().RequireAuthorization();
// A Swagger page at /docs for trying the API. The page itself is open, but it reads /openapi/v1.json with the
// browser's sign-in cookie, so it only lists the API once you are signed in.
app.UseSwaggerUI(swagger =>
{
    swagger.RoutePrefix = "docs";
    swagger.DocumentTitle = "ChefBot API";
    swagger.SwaggerEndpoint("/openapi/v1.json", "ChefBot API");
});
app.MapGet("/health", () => Results.Ok(new HealthResponse("ok"))).ExcludeFromDescription();

// Every API endpoint needs a signed-in user unless it is marked [AllowAnonymous].
app.MapWolverineEndpoints(endpoints => endpoints.RequireAuthorizeOnAll());

// Addresses such as /recipes/{id} or /settings/backups belong to the web page, so bookmarks and reloads work: any
// path that isn't a file gets index.html, which shows the matching view. Unknown /api paths still get a 404.
app.MapFallbackToFile("index.html", webPage).AllowAnonymous();
app.MapFallback("/api/{**rest}", () => Results.NotFound()).AllowAnonymous().ExcludeFromDescription();

// `dotnet run -- codegen write` writes the code Wolverine runs in Release builds; see CONTRIBUTING.md. It stops here,
// before anything touches the database.
if (args is ["codegen", ..])
{
    return await app.RunJasperFxCommands(args);
}

// ChefBot's tables are created before the message bus starts and creates its own.
await app.Services.PrepareChefBotAsync();
await app.StartAsync();
await app.Services.ApplyChefBotStartupSettingsAsync();
await app.WaitForShutdownAsync();
return 0;
