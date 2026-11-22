using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace ChefBot.Api.Endpoints;

/// <summary>
/// The meal plan as a calendar feed. Calendar apps can't sign in, so the feed's address carries a secret instead;
/// everyone signed in can see the address, and admins turn the feed on, replace its address or turn it off.
/// </summary>
public static class CalendarEndpoints
{
    /// <summary>
    /// The feed itself, as iCalendar text: the meals of the confirmed weeks from last week on. Needs no sign-in, only
    /// the secret in the address. 404 when the feed is off or the secret is wrong.
    /// </summary>
    [AllowAnonymous]
    [WolverineGet("/api/calendar/{token}/meals.ics")]
    public static async Task<IResult> Feed(string token, HttpContext http, [FromServices] IPlanManager plans, CancellationToken cancellationToken)
    {
        var text = await plans.GetCalendarAsync(token, Site(http), cancellationToken);
        http.Response.Headers.CacheControl = "private, max-age=300";
        return Results.Text(text, "text/calendar; charset=utf-8");
    }

    /// <summary>The feed's address to subscribe to, or <c>{"url": null}</c> when it is off.</summary>
    [WolverineGet("/api/calendar/feed")]
    public static Task<CalendarFeedResponse> Get(HttpContext http, [FromServices] IPlanManager plans, CancellationToken cancellationToken) =>
        plans.GetCalendarFeedAsync(Site(http), cancellationToken);

    /// <summary>Turns the feed on with a new address; one made before stops working. Admins only.</summary>
    [WolverinePost("/api/calendar/feed")]
    [Authorize(Policy = Sessions.AdminPolicy)]
    public static Task<CalendarFeedResponse> Start(HttpContext http, [FromServices] IPlanManager plans, CancellationToken cancellationToken) =>
        plans.StartCalendarFeedAsync(Site(http), cancellationToken);

    /// <summary>Turns the feed off. Admins only.</summary>
    [WolverineDelete("/api/calendar/feed")]
    [Authorize(Policy = Sessions.AdminPolicy)]
    public static Task<CalendarFeedResponse> Stop([FromServices] IPlanManager plans, CancellationToken cancellationToken) =>
        plans.StopCalendarFeedAsync(cancellationToken);

    /// <summary>When meals appear in the feed: each meal's start time, how long it lasts, and the slow cooker reminder.</summary>
    [WolverineGet("/api/calendar/times")]
    public static Task<CalendarTimesResponse> GetTimes([FromServices] IPlanManager plans, CancellationToken cancellationToken) =>
        plans.GetCalendarTimesAsync(cancellationToken);

    /// <summary>Changes when meals appear in the feed. Subscribed calendars pick it up when they next refresh. Admins only.</summary>
    [WolverinePut("/api/calendar/times")]
    [Authorize(Policy = Sessions.AdminPolicy)]
    public static Task<CalendarTimesResponse> SaveTimes(
        CalendarTimesRequest request, [FromServices] IPlanManager plans, CancellationToken cancellationToken) =>
        plans.SaveCalendarTimesAsync(request, cancellationToken);

    /// <summary>ChefBot's address as the request reached it (after a trusted proxy's forwarded headers), ending in a slash.</summary>
    private static Uri Site(HttpContext http) => new($"{http.Request.Scheme}://{http.Request.Host}{http.Request.PathBase}/");
}
