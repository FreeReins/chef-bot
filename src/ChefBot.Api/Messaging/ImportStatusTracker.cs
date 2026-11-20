using ChefBot.Core.Recipes;

namespace ChefBot.Api.Messaging;

/// <summary>The outcome of the most recent Mealie import.</summary>
/// <param name="State">Idle, Running, Succeeded or Failed.</param>
/// <param name="At">When the state last changed.</param>
/// <param name="Message">What went wrong, when it failed.</param>
/// <param name="Result">What changed, when it succeeded.</param>
public sealed record ImportStatus(string State, DateTimeOffset? At = null, string? Message = null, ImportResult? Result = null);

/// <summary>Remembers how the last background import went so the web page can show it. Kept in memory only.</summary>
public sealed class ImportStatusTracker
{
    private volatile ImportStatus _current = new("Idle");

    public ImportStatus Current => _current;

    public void Set(ImportStatus status) => _current = status;
}
