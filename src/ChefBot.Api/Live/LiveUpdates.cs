using System.Collections.Concurrent;
using System.Threading.Channels;

namespace ChefBot.Api.Live;

/// <summary>
/// Tells the open pages which part of the kitchen changed, so they load it again: "plan", "shopping", "pantry",
/// "recipes", "achievements" or "backups" (<see cref="LiveTopics"/>). Only the topic is sent, never data, so a page
/// always loads what it shows through the normal API, with its own sign-in.
/// </summary>
/// <remarks>
/// Lives in this process only: with more than one ChefBot server, a page would hear only about changes handled by
/// the server it is connected to, and this would need PostgreSQL LISTEN/NOTIFY between them.
/// </remarks>
public sealed class LiveUpdates
{
    private readonly ConcurrentDictionary<Guid, Channel<string>> listeners = new();

    /// <summary>Sends a topic to every open page. A page that is slow to read only misses repeats of a topic.</summary>
    public void Broadcast(string topic)
    {
        foreach (var listener in listeners.Values)
        {
            listener.Writer.TryWrite(topic);
        }
    }

    /// <summary>The topics sent from now until the page goes away.</summary>
    public async IAsyncEnumerable<string> ListenAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(32)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
        });
        listeners[id] = channel;
        try
        {
            await foreach (var topic in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return topic;
            }
        }
        finally
        {
            listeners.TryRemove(id, out _);
        }
    }
}
