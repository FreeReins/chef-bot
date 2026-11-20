using System.Text.Json.Serialization;
using ChefBot.Api;
using ChefBot.Api.Messaging;
using Wolverine;
using Wolverine.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddChefBot(builder.Configuration);
builder.Services.AddChefBotSignIn(builder.Configuration);

// HTTP always; HTTPS too when Https:Mode is SelfSigned or Certificate.
var certificates = builder.Services.AddChefBotHttps(builder.Configuration);
builder.WebHost.ConfigureKestrel(certificates.Configure);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ChefBotExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(json =>
    json.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Wolverine runs the HTTP endpoints (Endpoints/), the background message
// handlers (Messaging/) and the weekly schedule.
builder.Host.UseWolverine(wolverine => wolverine.ResolveChefBotServicesFromContainer());
builder.Services.AddWolverineHttp();

var app = builder.Build();

app.UseExceptionHandler();
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
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).ExcludeFromDescription();

// Every API endpoint needs a signed-in user unless it is marked [AllowAnonymous].
app.MapWolverineEndpoints(endpoints => endpoints.RequireAuthorizeOnAll());

// Addresses such as /recipes/{id} or /settings/mealie belong to the web page, so bookmarks and reloads work: any
// path that isn't a file gets index.html, which shows the matching view. Unknown /api paths still get a 404.
app.MapFallbackToFile("index.html", webPage).AllowAnonymous();
app.MapFallback("/api/{**rest}", () => Results.NotFound()).AllowAnonymous().ExcludeFromDescription();

await app.Services.PrepareChefBotAsync();
await app.StartAsync();
await app.Services.ScheduleNextWeeklyPlanAsync();
await app.WaitForShutdownAsync();
