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
    /// "backups" or "history"), sent when that part changed, by anyone. Load it again with the normal API. A "ping" is
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

        await using var topics = live.ListenAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);
        var next = topics.MoveNextAsync().AsTask();
        while (!cancellationToken.IsCancellationRequested)
        {
            var ping = Task.Delay(PingEvery, cancellationToken);
            Task finished;
            try
            {
                finished = await Task.WhenAny(next, ping);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }

            if (finished == ping)
            {
                if (ping.IsCanceled)
                {
                    yield break;
                }

                yield return new SseItem<string>(string.Empty, "ping");
                continue;
            }

            bool more;
            try
            {
                more = await next;
            }
            catch (OperationCanceledException)
            {
                yield break;
            }

            if (!more)
            {
                yield break;
            }

            yield return new SseItem<string>(string.Empty, topics.Current);
            next = topics.MoveNextAsync().AsTask();
        }
    }
}
