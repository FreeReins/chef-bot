namespace ChefBot.Api.Logging;

/// <summary>
/// Keeps the most recent log entries in memory since startup, so admins can read them on the Settings tab without
/// opening the server's console. Older entries are dropped once <see cref="Capacity"/> is reached.
/// </summary>
public sealed class LogBuffer
{
    /// <summary>How many entries are kept.</summary>
    public const int Capacity = 1000;

    private readonly Queue<LogEntry> entries = new(Capacity);
    private readonly Lock gate = new();
    private long sequence;

    /// <summary>Adds an entry, giving it the next sequence number.</summary>
    public void Add(DateTimeOffset time, string level, string category, string message, string? exception)
    {
        lock (gate)
        {
            if (entries.Count == Capacity)
            {
                entries.Dequeue();
            }

            entries.Enqueue(new LogEntry(++sequence, time, level, category, message, exception));
        }
    }

    /// <summary>The entries with a sequence number above <paramref name="after"/>, oldest first.</summary>
    public IReadOnlyList<LogEntry> Since(long after)
    {
        lock (gate)
        {
            return [.. entries.Where(entry => entry.Sequence > after)];
        }
    }
}
