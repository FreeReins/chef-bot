namespace ChefBot.Core.Utilities;

/// <summary>
/// Lets one change to a plan finish before the next one starts. Two refreshes of the same plan at once (the web
/// page's own refresh and the one that follows a pantry change) would otherwise both replace its shopping list.
/// ChefBot runs as a single instance per database, so a lock in memory is enough.
/// </summary>
internal sealed class PlanWriteLock
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<IDisposable> AcquireAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        return new Release(_gate);
    }

    private sealed class Release(SemaphoreSlim gate) : IDisposable
    {
        private int _released;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                gate.Release();
            }
        }
    }
}
