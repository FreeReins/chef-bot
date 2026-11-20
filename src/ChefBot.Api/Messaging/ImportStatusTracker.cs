namespace ChefBot.Api.Messaging;

/// <summary>Remembers how the last background import went so the web page can show it. Kept in memory only.</summary>
public sealed class ImportStatusTracker
{
    private volatile ImportStatus _current = new("Idle");

    public ImportStatus Current => _current;

    public void Set(ImportStatus status) => _current = status;
}
