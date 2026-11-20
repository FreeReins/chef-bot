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
app.UseChefBotHttpsRedirect();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapOpenApi().RequireAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).ExcludeFromDescription();

// Every API endpoint needs a signed-in user unless it is marked [AllowAnonymous].
app.MapWolverineEndpoints(endpoints => endpoints.RequireAuthorizeOnAll());

await app.Services.PrepareChefBotAsync();
await app.StartAsync();
await app.Services.ScheduleNextWeeklyPlanAsync();
await app.WaitForShutdownAsync();
