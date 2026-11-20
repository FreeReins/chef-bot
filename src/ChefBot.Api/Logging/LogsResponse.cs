namespace ChefBot.Api.Logging;

/// <summary>The recent log entries the server still holds, oldest first.</summary>
/// <param name="Entries">The entries newer than the one asked for.</param>
/// <param name="Capacity">How many entries are kept; older ones are dropped.</param>
public sealed record LogsResponse(IReadOnlyList<LogEntry> Entries, int Capacity);
