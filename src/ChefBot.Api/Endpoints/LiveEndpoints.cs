using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using ChefBot.Api.Live;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace ChefBot.Api.Endpoints;

/// <summary>A stream that tells the open page what changed, so it never has to poll.</summary>
public static class LiveEndpoints
{
    private static readonly TimeSpan PingEvery = TimeSpan.FromSeconds(25);

    private static readonly TimeSpan ReconnectAfter = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Server-sent events: each event's name is a topic ("plan", "shopping", "pantry", "recipes", "achievements",
    /// "backups", "history" or "inbox"), sent when that part changed, by anyone. Load it again with the normal API. A "ping" is
    /// sent every 25 seconds to keep proxies from closing the connection. After reconnecting, load everything, since
    /// topics sent meanwhile are not repeated.
    /// </summary>
    [WolverineGet("/api/live")]
    public static IResult Get(HttpContext http, [FromServices] LiveUpdates live)
    {
        http.Response.Headers.CacheControl = "no-store";
        http.Response.Headers["X-Accel-Buffering"] = "no";
        return TypedResults.ServerSentEvents(EventsAsync(live, http.RequestAborted));
    }

    private static async IAsyncEnumerable<SseItem<string>> EventsAsync(
        LiveUpdates live, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return new SseItem<string>(string.Empty, "ping") { ReconnectionInterval = ReconnectAfter };

        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await using var topics = live.ListenAsync(stop.Token).GetAsyncEnumerator(stop.Token);
        var next = topics.MoveNextAsync().AsTask();
        try
        {
            while (true)
            {
                var ping = Task.Delay(PingEvery, stop.Token);
                var finished = await Task.WhenAny(next, ping);
                if (!finished.IsCompletedSuccessfully)
                {
                    yield break; // The page went away.
                }

                if (finished == ping)
                {
                    yield return new SseItem<string>(string.Empty, "ping");
                    continue;
                }

                if (!await next)
                {
                    yield break;
                }

                yield return new SseItem<string>(string.Empty, topics.Current);
                next = topics.MoveNextAsync().AsTask();
            }
        }
        finally
        {
            // Disposing topics while a MoveNextAsync is still running throws NotSupportedException, so stop it first.
            stop.Cancel();
            await ((Task)next).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }
}
