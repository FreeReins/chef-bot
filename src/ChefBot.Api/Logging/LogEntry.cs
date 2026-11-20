namespace ChefBot.Api.Logging;

/// <summary>One line written to the log, as the Logs section of the Settings tab shows it.</summary>
/// <param name="Sequence">Rises by one for each entry since startup, so a reader can ask for only the newer ones.</param>
/// <param name="Time">When it was written.</param>
/// <param name="Level">Trace, Debug, Information, Warning, Error or Critical.</param>
/// <param name="Category">The part of ChefBot that wrote it, such as ChefBot.Https.</param>
/// <param name="Message">The message, with its values filled in.</param>
/// <param name="Exception">The exception and its stack trace, when one was logged.</param>
public sealed record LogEntry(long Sequence, DateTimeOffset Time, string Level, string Category, string Message, string? Exception);
