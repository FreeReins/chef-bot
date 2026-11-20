namespace ChefBot.Api;

/// <summary>The answer from <c>/health</c>: <c>{ "status": "ok" }</c>.</summary>
public sealed record HealthResponse(string Status);
